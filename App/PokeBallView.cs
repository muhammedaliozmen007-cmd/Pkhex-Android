using Android.Content;
using Android.Graphics;
using Android.Views;

namespace PKHeX.Android.App;

internal sealed class PokeBallView(Context context) : View(context)
{
    protected override void OnDraw(Canvas canvas)
    {
        base.OnDraw(canvas);
        var size = Math.Min(Width, Height) * .86f;
        var cx = Width / 2f; var cy = Height / 2f; var r = size / 2f;
        using var paint = new Paint(PaintFlags.AntiAlias);
        paint.Color = Color.Rgb(33, 61, 77);
        canvas.DrawCircle(cx, cy, r, paint);
        paint.Color = Color.Rgb(77, 224, 197);
        using var rect = new RectF(cx-r, cy-r, cx+r, cy+r);
        canvas.DrawArc(rect, 180, 180, true, paint);
        paint.Color = Color.Rgb(12, 21, 36); paint.StrokeWidth = size * .095f;
        canvas.DrawLine(cx-r,cy,cx+r,cy,paint);
        canvas.DrawCircle(cx,cy,size*.2f,paint);
        paint.Color = Color.Rgb(229, 247, 243);
        canvas.DrawCircle(cx,cy,size*.11f,paint);
    }
}
