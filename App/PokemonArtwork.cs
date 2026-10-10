using Android.Content.Res;
using Android.Graphics;
using Android.Widget;
using PKHeX.Core;
using System.Text.Json;
using System.Runtime.CompilerServices;

namespace PKHeX.Android.App;

/// <summary>Offline HOME artwork. Image IDs and PKHeX form numbers are separate namespaces.</summary>
internal sealed class PokemonArtwork
{
    private readonly AssetManager assets;
    private readonly HashSet<string> files;
    private readonly Dictionary<string, string> forms;
    private const long CacheBudget = 24 * 1024 * 1024;
    private readonly object cacheLock = new();
    private readonly SemaphoreSlim decodeSlots = new(2);
    private readonly Dictionary<string, (Bitmap Bitmap, LinkedListNode<string> Node)> cache = [];
    private readonly LinkedList<string> order = [];
    private long cacheBytes;
    private sealed class PendingImage
    {
        internal readonly object Gate = new();
        internal CancellationTokenSource? Current;
    }
    private readonly ConditionalWeakTable<ImageView, PendingImage> requests = new();
    internal PokemonArtwork(AssetManager assets)
    {
        this.assets = assets;
        using var stream = assets.Open("art/index.json");
        using var json = JsonDocument.Parse(stream);
        files = json.RootElement.GetProperty("files").EnumerateArray().Select(x => x.GetString()!).ToHashSet();
        forms = json.RootElement.GetProperty("forms").EnumerateObject().ToDictionary(x => x.Name, x => x.Value.GetString()!);
    }

    private string? Resolve(ushort species, byte form, bool shiny, byte gender, EntityContext context)
    {
        if (!forms.TryGetValue($"{context}:{species}:{form}", out var id) && !forms.TryGetValue($"{species}:{form}", out id))
            id = species.ToString();
        // Female art is optional; it must never prevent a normal picture from being shown.
        string[] candidates = gender == 1
            ? [shiny ? $"shiny/female/{id}.webp" : $"female/{id}.webp", shiny ? $"shiny/{id}.webp" : $"{id}.webp", $"{id}.webp"]
            : [shiny ? $"shiny/{id}.webp" : $"{id}.webp", $"{id}.webp"];
        return candidates.FirstOrDefault(files.Contains);
    }

    // Fixed view sizes avoid layout jumps. Decode on workers, never while scrolling.
    internal void Bind(ImageView image, int pixels, CancellationToken cancel, ushort species,
        byte form = 0, bool shiny = false, byte gender = 2, EntityContext context = EntityContext.Gen9)
    {
        var pending = requests.GetValue(image, _ => new PendingImage());
        lock (pending.Gate)
        {
            pending.Current?.Cancel(); pending.Current?.Dispose(); pending.Current = null;
        }
        var path = Resolve(species, form, shiny, gender, context);
        var sample = pixels <= 96 ? 4 : pixels <= 192 ? 2 : 1;
        var key = path + ":" + sample;
        image.Tag = new Java.Lang.String(key);
        if (path is null) { image.SetImageDrawable(null); return; }
        if (Cached(key) is { } ready) { image.SetImageBitmap(ready); return; }
        image.SetImageDrawable(null);
        var request = CancellationTokenSource.CreateLinkedTokenSource(cancel);
        lock (pending.Gate) pending.Current = request;
        _ = BindAsync(image, path, key, sample, request.Token, cancel, pending, request);
    }

    private async Task BindAsync(ImageView image, string path, string key, int sample, CancellationToken cancel, CancellationToken lifetime, PendingImage pending, CancellationTokenSource request)
    {
        try
        {
            var bitmap = await DecodeAsync(path, key, sample, cancel).ConfigureAwait(false);
            if (bitmap is null || cancel.IsCancellationRequested || lifetime.IsCancellationRequested) return;
            image.Post(() =>
            {
                if (!cancel.IsCancellationRequested && !lifetime.IsCancellationRequested && image.Handle != IntPtr.Zero && image.Tag?.ToString() == key)
                    image.SetImageBitmap(bitmap);
            });
        }
        catch (OperationCanceledException) { }
        catch (Exception ex) { global::Android.Util.Log.Warn("PKHeX-Artwork", ex.Message); }
        finally
        {
            lock (pending.Gate) if (ReferenceEquals(pending.Current, request)) pending.Current = null;
            request.Dispose();
        }
    }

    private Bitmap? Cached(string key)
    {
        lock (cacheLock)
        {
            if (!cache.TryGetValue(key, out var entry)) return null;
            order.Remove(entry.Node); order.AddLast(entry.Node); return entry.Bitmap;
        }
    }

    private async Task<Bitmap?> DecodeAsync(string path, string key, int sample, CancellationToken cancel)
    {
        await decodeSlots.WaitAsync(cancel).ConfigureAwait(false);
        try
        {
            cancel.ThrowIfCancellationRequested();
            if (Cached(key) is { } cached) return cached;
            var bitmap = await Task.Run(() =>
            {
                using var data = assets.Open("art/" + path);
                using var options = new BitmapFactory.Options { InSampleSize = sample, InScaled = false };
                return BitmapFactory.DecodeStream(data, null, options);
            }, cancel).ConfigureAwait(false);
            if (bitmap is null) return null;
            lock (cacheLock)
            {
                if (cache.TryGetValue(key, out var existing)) { bitmap.Dispose(); return existing.Bitmap; }
                var node = order.AddLast(key); cache.Add(key, (bitmap, node)); cacheBytes += bitmap.ByteCount;
                while (cacheBytes > CacheBudget && order.First is { } oldest)
                {
                    cacheBytes -= cache[oldest.Value].Bitmap.ByteCount;
                    cache.Remove(oldest.Value); order.RemoveFirst();
                    // Do not recycle: an ImageView may still be displaying this bitmap.
                }
            }
            return bitmap;
        }
        finally { decodeSlots.Release(); }
    }
    internal bool HasExactForm(PKM pk) => pk.Form == 0 || forms.ContainsKey($"{pk.Context}:{pk.Species}:{pk.Form}") || forms.ContainsKey($"{pk.Species}:{pk.Form}");
}
