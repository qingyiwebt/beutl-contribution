﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Beutl.Api;
using Beutl.Api.Objects;
using Beutl.Api.Services;

using NuGet.Packaging.Core;
using NuGet.Versioning;

using Reactive.Bindings;

namespace BeUtl.ViewModels.ExtensionsPages.DiscoverPages;

public sealed class PublicPackageDetailsPageViewModel : BasePageViewModel
{
    private readonly CompositeDisposable _disposables = new();
    private readonly InstalledPackageRepository _installedPackageRepository;
    private readonly PackageChangesQueue _queue;
    private readonly PackageManager _manager;
    private readonly BeutlApiApplication _app;

    public PublicPackageDetailsPageViewModel(Package package, BeutlApiApplication app)
    {
        Package = package;
        _app = app;
        _installedPackageRepository = app.GetResource<InstalledPackageRepository>();
        _manager = app.GetResource<PackageManager>();
        _queue = app.GetResource<PackageChangesQueue>();

        Refresh = new AsyncReactiveCommand(IsBusy.Not())
            .WithSubscribe(async () =>
            {
                try
                {
                    IsBusy.Value = true;
                    await package.RefreshAsync();
                    int totalCount = 0;
                    int prevCount = 0;

                    do
                    {
                        Release[] array = await package.GetReleasesAsync(totalCount, 30);
                        if (Array.Find(array, x => x.IsPublic.Value) is { } publicRelease)
                        {
                            await publicRelease.RefreshAsync();
                            LatestRelease.Value = publicRelease;
                            break;
                        }

                        totalCount += array.Length;
                        prevCount = array.Length;
                    } while (prevCount == 30);
                }
                catch (Exception e)
                {
                    ErrorHandle(e);
                }
                finally
                {
                    IsBusy.Value = false;
                }
            })
            .DisposeWith(_disposables);

        Refresh.Execute();

        IObservable<PackageChangesQueue.EventType> observable = _queue.GetObservable(package.Name);
        CanCancel = observable.Select(x => x != PackageChangesQueue.EventType.None)
            .ToReadOnlyReactivePropertySlim()
            .DisposeWith(_disposables);

        IObservable<bool> installed = _installedPackageRepository.GetObservable(package.Name);
        IsInstallButtonVisible = installed
            .AnyTrue(CanCancel)
            .Not()
            .ToReadOnlyReactivePropertySlim()
            .DisposeWith(_disposables);

        IsUninstallButtonVisible = installed
            .AreTrue(CanCancel.Not())
            .ToReadOnlyReactivePropertySlim()
            .DisposeWith(_disposables);

        Install = new AsyncReactiveCommand(IsBusy.Not())
            .WithSubscribe(async () =>
            {
                try
                {
                    IsBusy.Value = true;
                    await _app.AuthorizedUser.Value!.RefreshAsync();
                    Release? release = (await package.GetReleasesAsync(0, 1)).FirstOrDefault();
                    if (release != null)
                    {
                        var packageId = new PackageIdentity(Package.Name, new NuGetVersion(release.Version.Value));
                        _queue.InstallQueue(packageId);
                        Notification.Show(new Notification(
                            Title: "パッケージインストーラー",
                            Message: $"'{packageId}'のインストールを予約しました。\nパッケージの変更を適用するには、Beutlを終了してください。"));
                    }
                }
                catch (Exception e)
                {
                    ErrorHandle(e);
                }
                finally
                {
                    IsBusy.Value = false;
                }
            })
            .DisposeWith(_disposables);

        Update = new AsyncReactiveCommand(IsBusy.Not())
            .WithSubscribe(async () =>
            {
                try
                {
                    IsBusy.Value = true;
                    await _app.AuthorizedUser.Value!.RefreshAsync();
                    Release? release = (await package.GetReleasesAsync(0, 1)).FirstOrDefault();
                    if (release != null)
                    {
                        var packageId = new PackageIdentity(Package.Name, new NuGetVersion(release.Version.Value));
                        _queue.InstallQueue(packageId);
                        Notification.Show(new Notification(
                            Title: "パッケージインストーラー",
                            Message: $"'{packageId}'のインストールを予約しました。\nパッケージの変更を適用するには、Beutlを終了してください。"));
                    }
                }
                catch (Exception e)
                {
                    ErrorHandle(e);
                }
                finally
                {
                    IsBusy.Value = false;
                }
            })
            .DisposeWith(_disposables);

        Uninstall = new ReactiveCommand(IsBusy.Not())
            .WithSubscribe(() =>
            {
                try
                {
                    IsBusy.Value = true;
                    foreach (PackageIdentity item in _installedPackageRepository.GetLocalPackages(Package.Name))
                    {
                        _queue.UninstallQueue(item);
                        Notification.Show(new Notification(
                            Title: "パッケージインストーラー",
                            Message: $"'{item}'のアンインストールを予約しました。\nパッケージの変更を適用するには、Beutlを終了してください。"));
                    }
                }
                catch (Exception e)
                {
                    ErrorHandle(e);
                }
                finally
                {
                    IsBusy.Value = false;
                }
            })
            .DisposeWith(_disposables);

        Cancel = new ReactiveCommand()
            .WithSubscribe(() =>
            {
                try
                {
                    IsBusy.Value = true;
                    _queue.Cancel(Package.Name);
                }
                catch (Exception e)
                {
                    ErrorHandle(e);
                }
                finally
                {
                    IsBusy.Value = false;
                }
            })
            .DisposeWith(_disposables);
    }

    public Package Package { get; }

    public ReactivePropertySlim<Release> LatestRelease { get; } = new();

    public ReadOnlyReactivePropertySlim<bool> IsInstallButtonVisible { get; }

    public ReadOnlyReactivePropertySlim<bool> IsUninstallButtonVisible { get; }

    public ReactivePropertySlim<bool> IsUpdateButtonVisible { get; } = new();

    public ReadOnlyReactivePropertySlim<bool> CanCancel { get; }

    public AsyncReactiveCommand Install { get; }

    public AsyncReactiveCommand Update { get; }

    public ReactiveCommand Uninstall { get; }

    public ReactiveCommand Cancel { get; }

    public ReactivePropertySlim<bool> IsBusy { get; } = new();

    public AsyncReactiveCommand Refresh { get; }

    public override void Dispose()
    {
        _disposables.Dispose();
    }
}
