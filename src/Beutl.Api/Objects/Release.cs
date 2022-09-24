﻿using System.Reactive.Linq;

using Reactive.Bindings;

namespace Beutl.Api.Objects;

public class Release
{
    private readonly BeutlClients _clients;
    private readonly ReactivePropertySlim<ReleaseResponse> _response;
    private readonly ReactivePropertySlim<bool> _isDeleted = new();

    public Release(Package package, ReleaseResponse response, BeutlClients clients)
    {
        Package = package;
        _clients = clients;
        _response = new ReactivePropertySlim<ReleaseResponse>(response);

        Version = _response.Select(x => new Version(x.Version)).ToReadOnlyReactivePropertySlim()!;
        Title = _response.Select(x => x.Title).ToReadOnlyReactivePropertySlim()!;
        Body = _response.Select(x => x.Body).ToReadOnlyReactivePropertySlim()!;
        AssetId = _response.Select(x => x.Asset_id).ToReadOnlyReactivePropertySlim()!;
        IsPublic = _response.Select(x => x.Public).ToReadOnlyReactivePropertySlim()!;
    }

    public Package Package { get; }

    public long Id { get; }

    public IReadOnlyReactiveProperty<ReleaseResponse> Response => _response;

    public IReadOnlyReactiveProperty<Version> Version { get; }

    public IReadOnlyReactiveProperty<string> Title { get; }

    public IReadOnlyReactiveProperty<string> Body { get; }

    public IReadOnlyReactiveProperty<long?> AssetId { get; }

    public IReadOnlyReactiveProperty<bool> IsPublic { get; }

    public IReadOnlyReactiveProperty<bool> IsDeleted => _isDeleted;

    public async Task RefreshAsync()
    {
        _response.Value = await _clients.Releases.GetReleaseAsync(
            Package.Owner.Name.Value, Package.Name.Value, _response.Value.Version);

        _isDeleted.Value = false;
    }

    public async Task UpdateAsync(UpdateReleaseRequest request)
    {
        if (_isDeleted.Value)
        {
            throw new InvalidOperationException("This object has been deleted.");
        }

        _response.Value = await _clients.Releases.PatchAsync(
            Package.Owner.Name.Value,
            Package.Name.Value,
            Response.Value.Version,
            request);
    }

    public async Task DeleteAsync()
    {
        FileResponse response = await _clients.Releases.DeleteAsync(
            Package.Owner.Name.Value,
            Package.Name.Value,
            Response.Value.Version);

        response.Dispose();

        _isDeleted.Value = true;
    }

    public async Task<ReleaseResource> AddResourceAsync(string locale, CreateReleaseResourceRequest request)
    {
        ReleaseResourceResponse response = await _clients.ReleaseResources.PostAsync(
            Package.Owner.Name.Value,
            Package.Name.Value,
            Response.Value.Version,
            locale,
            request);

        return new ReleaseResource(this, response, _clients);
    }

    public async Task<ReleaseResource[]> GetResourcesAsync()
    {
        return (await _clients.ReleaseResources.GetResourcesAsync(Package.Owner.Name.Value, Package.Name.Value, Response.Value.Version))
            .Select(x => new ReleaseResource(this, x, _clients))
            .ToArray();
    }

    public async Task<Asset> GetAssetAsync()
    {
        if (!AssetId.Value.HasValue)
            throw new InvalidOperationException("This release has no assets.");

        AssetMetadataResponse response = await _clients.Assets.GetAsset2Async(AssetId.Value.Value);
        return new Asset(Package.Owner, response, _clients);
    }
}

