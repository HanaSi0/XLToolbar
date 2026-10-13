using XLToolbar;

var checks = 0;
void Check(bool valid, string message)
{
    checks++;
    if (!valid) throw new Exception(message);
}
void Validate(float width, float gap, float[] left, float[] right)
{
    var layout = TopBarLayout.Arrange(width, gap, left, right);
    var available = float.IsFinite(width) ? Math.Max(1, width) : 320;
    Check(layout.Slots.Count == left.Length + right.Length, "Missing shortcuts");
    Check(layout.Rows >= 1, "Invalid row count");
    Check(layout.Slots.Select(x => (x.Right, x.Index)).Distinct().Count() == layout.Slots.Count, "Duplicate shortcuts");
    foreach (var slot in layout.Slots)
    {
        Check(float.IsFinite(slot.X) && float.IsFinite(slot.Width), "Nonfinite geometry");
        Check(slot.Width > 0 && slot.X >= -.001f && slot.X + slot.Width <= available + .001f, "Shortcut outside viewport");
        Check(slot.Row >= 0 && slot.Row < layout.Rows, "Invalid row");
    }
    foreach (var row in layout.Slots.GroupBy(x => x.Row))
    {
        var slots = row.OrderBy(x => x.X).ToArray();
        for (var i = 1; i < slots.Length; i++)
            Check(slots[i - 1].X + slots[i - 1].Width <= slots[i].X + .001f, "Overlapping shortcuts");
    }
}
foreach (var width in new[] { 1f, 40, 320, 800, 1920, 3840, float.NaN })
foreach (var gap in new[] { 0f, 8, 24, float.NaN })
{
    Validate(width, gap, [], []);
    Validate(width, gap, [120, 80, 2000, 90, float.NaN, -20], [80, 140, 100]);
    Validate(width, gap, [80, 90], []);
    Validate(width, gap, [], [80, 140, float.PositiveInfinity]);
}
var random = new Random(17);
for (var i = 0; i < 250; i++)
    Validate(random.Next(320, 3841), random.Next(25),
        Enumerable.Range(0, random.Next(1, 30)).Select(_ => (float)random.Next(30, 500)).ToArray(),
        Enumerable.Range(0, random.Next(10)).Select(_ => (float)random.Next(30, 500)).ToArray());
var pinned = TopBarLayout.Arrange(1000, 8, [100, 100], [120, 80]);
Check(Math.Abs(pinned.Slots.Where(x => x.Right).Max(x => x.X + x.Width) - 1000) < .001, "Pinned group not right aligned");
Check(TopIconPolicy.Find(new[] { 1, 2 }, x => x == 3, _ => throw new Exception("Dereferenced missing row")) == 0, "Missing icon fallback");
Check(TopIconPolicy.Find(new[] { 1, 2 }, x => x == 2, x => x * 100) == 200, "Wrong icon match");
Console.WriteLine($"Passed {checks} assertions: wrapping, pinning, bounds, non-overlap, and missing icon fallback.");
