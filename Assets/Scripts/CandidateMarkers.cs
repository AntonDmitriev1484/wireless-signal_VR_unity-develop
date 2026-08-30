/*
 *  Shared helpers for the colour-coded MCQ candidate markers.
 *
 *  Where an MCQ's options are receiver locations (Task 1's phone_optimization, Task 2's
 *  interference question), each option gets a small transparent cube dropped on its receiver
 *  position, tinted with that option's material (M_obj1..M_obj4) - the same colour coding the
 *  movable furniture already uses in Task 1. The matching answer button carries the same colour.
 */

using System;
using TMPro;
using UnityEngine;

public static class CandidateMarkers
{
    // Edge length of the cube dropped on a candidate receiver location.
    public const float CUBE_SIZE = 0.5f;

    // The cube is plain white and barely there - it only marks WHERE the option is. Which option it
    // is comes from the coloured letter badge above it, so the cube carries no colour of its own.
    public static readonly Color CUBE_COLOR = Color.white;

    // Opacity of a candidate cube, unselected and selected. The selected cube stays translucent so
    // the receiver marker inside it remains visible.
    public const float ALPHA_NORMAL = 0.08f;
    public const float ALPHA_SELECTED = 0.22f;

    // Letter badge floating above each cube.
    public const float BADGE_HEIGHT = 0.4f;      // above the candidate location
    public const float BADGE_SIZE = 0.3f;
    public const float BADGE_THICKNESS = 0.02f;
    public const float BADGE_FONT_SIZE = 3f;

    // A transparent cube marking one option's receiver location.
    public static GameObject SpawnCube(Vector3 position, Material material, string name)
    {
        GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.name = name;
        cube.transform.position = position;
        cube.transform.localScale = Vector3.one * CUBE_SIZE;

        // Purely a visual marker - it must never absorb clicks or raycasts.
        Collider col = cube.GetComponent<Collider>();
        if (col != null) col.enabled = false;

        cube.GetComponent<MeshRenderer>().sharedMaterial = material;
        return cube;
    }

    // A label floating BADGE_HEIGHT above a candidate location: the option's letter in black on a
    // thin slab in that option's colour, tying the marker on the floor to the button of the same
    // colour. The whole badge turns to face the user via FaceCamera.
    //
    // A slab rather than a sprite: it is closed geometry, so it reads from any angle without
    // worrying about backface culling, and it needs no sprite asset.
    public static GameObject SpawnBadge(
        Vector3 position,
        char letter,
        Color color,
        Material template,
        string name)
    {
        GameObject badge = new GameObject(name);
        badge.transform.position = position + Vector3.up * BADGE_HEIGHT;

        // Same camera-facing treatment the message text uses.
        badge.AddComponent<FaceCamera>();

        GameObject backdrop = GameObject.CreatePrimitive(PrimitiveType.Cube);
        backdrop.name = "Backdrop";
        backdrop.transform.SetParent(badge.transform, false);
        backdrop.transform.localScale = new Vector3(BADGE_SIZE, BADGE_SIZE, BADGE_THICKNESS);

        Collider col = backdrop.GetComponent<Collider>();
        if (col != null) col.enabled = false;

        // Fully opaque, so the black letter stays legible over whatever is behind the badge.
        backdrop.GetComponent<MeshRenderer>().sharedMaterial = Tint(template, color, 1f);

        GameObject letterObject = new GameObject("Letter");
        letterObject.transform.SetParent(badge.transform, false);

        // FaceCamera points the badge's forward AWAY from the viewer, so -Z is the side they see:
        // put the letter just clear of the slab's front face on that side.
        letterObject.transform.localPosition =
            new Vector3(0f, 0f, -(BADGE_THICKNESS * 0.5f + 0.005f));

        TextMeshPro text = letterObject.AddComponent<TextMeshPro>();
        text.text = letter.ToString();
        text.fontSize = BADGE_FONT_SIZE;
        text.alignment = TextAlignmentOptions.Center;
        text.color = Color.black;

        return badge;
    }

    // Runtime copy of a material at a given colour and opacity. Project assets are never mutated.
    public static Material Tint(Material src, Color color, float alpha)
    {
        Material mat = new Material(src);

        Color c = color;
        c.a = alpha;

        mat.color = c;
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", c);

        return mat;
    }

    // Base colour of an option material (M_obj1..M_obj4).
    public static Color OptionColor(Material optionMaterial)
    {
        if (optionMaterial == null) return Color.white;

        return optionMaterial.HasProperty("_BaseColor")
            ? optionMaterial.GetColor("_BaseColor")
            : optionMaterial.color;
    }

    // World position of one receiver in a condition's path CSV: the last coordinate of the first
    // row whose Rx_Number matches. Field layout mirrors MoveAsParticleTest1_v2.LoadDataFromCSVLine.
    public static bool TryReadRxPosition(string condition, int rxNum, out Vector3 position)
    {
        position = Vector3.zero;

        TextAsset ta = Resources.Load<TextAsset>(condition);
        if (ta == null) return false;

        string[] lines = ta.text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

        for (int i = 1; i < lines.Length; i++)    // skip the header
        {
            string[] fields = lines[i].Split(',');
            if (fields.Length < 6) continue;

            if (!int.TryParse(fields[0].Trim(), out int rx) || rx != rxNum) continue;

            string coords = string.Join(",", fields, 5, fields.Length - 5).Trim().Trim('"');
            string[] points = coords.Split(',');

            string[] xyz = points[points.Length - 1].Trim()
                .Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (xyz.Length < 3) continue;

            var inv = System.Globalization.CultureInfo.InvariantCulture;
            var style = System.Globalization.NumberStyles.Float;

            if (float.TryParse(xyz[0], style, inv, out float x) &&
                float.TryParse(xyz[1], style, inv, out float y) &&
                float.TryParse(xyz[2], style, inv, out float z))
            {
                position = new Vector3(x, y, z);
                return true;
            }
        }

        return false;
    }
}
