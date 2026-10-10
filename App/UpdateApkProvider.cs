// SPDX-License-Identifier: GPL-3.0-or-later
using Android.App;
using Android.Content;
using Android.Database;
using Android.OS;
using Uri = Android.Net.Uri;

namespace PKHeX.Android.App;

/// <summary>Only the downloaded update APK can be read; save files are never shared.</summary>
[ContentProvider([Authority], Exported = false, GrantUriPermissions = true)]
public sealed class UpdateApkProvider : ContentProvider
{
    public const string Authority = "org.pkhex.android.community.updates";
    internal static string ApkPath(Context context) => System.IO.Path.Combine(context.FilesDir!.AbsolutePath, "updates", "latest.apk");
    public override bool OnCreate() => true;

    private string Resolve(Uri uri)
    {
        if (uri.Scheme != "content" || uri.Authority != Authority || uri.Path != "/apk/latest.apk" || uri.Query is not null || uri.Fragment is not null)
            throw new Java.IO.FileNotFoundException("Unknown update file");
        var path = ApkPath(Context!);
        if (!System.IO.File.Exists(path)) throw new Java.IO.FileNotFoundException("Update APK not found");
        return path;
    }

    public override string? GetType(Uri uri) { Resolve(uri); return "application/vnd.android.package-archive"; }
    public override ParcelFileDescriptor? OpenFile(Uri uri, string mode)
    {
        if (mode != "r") throw new Java.Lang.SecurityException("Update APK is read-only");
        return ParcelFileDescriptor.Open(new Java.IO.File(Resolve(uri)), ParcelFileMode.ReadOnly);
    }
    public override ICursor? Query(Uri uri, string[]? projection, string? selection, string[]? selectionArgs, string? sortOrder)
    {
        var file = new System.IO.FileInfo(Resolve(uri)); var columns = projection ?? ["_display_name", "_size"];
        var cursor = new MatrixCursor(columns); var row = cursor.NewRow()!;
        foreach (var column in columns)
        {
            if (column == "_display_name") row.Add("PKHeX-Android-Update.apk");
            else if (column == "_size") row.Add(Java.Lang.Long.ValueOf(file.Length));
            else row.Add(null);
        }
        return cursor;
    }
    public override Uri? Insert(Uri uri, ContentValues? values) => throw new Java.Lang.SecurityException("Read-only provider");
    public override int Delete(Uri uri, string? selection, string[]? selectionArgs) => throw new Java.Lang.SecurityException("Read-only provider");
    public override int Update(Uri uri, ContentValues? values, string? selection, string[]? selectionArgs) => throw new Java.Lang.SecurityException("Read-only provider");
}
