using System.Text;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;

/// <summary>
/// Ruby text support for TextMeshPro via ITextPreprocessor.
///
/// Use the custom tag directly in any TMP text field (Inspector or code):
///   <ruby=annotation>base text</ruby>
///
/// Examples:
///   <ruby=pronunciation>Hello</ruby>          ← English
///   <ruby=한자>漢字</ruby>                      ← Korean
///   "오늘은 <ruby=한자>漢字</ruby>를 배웁니다."  ← Mixed
///
/// Centering uses actual glyph metrics from the TMP font asset,
/// so it works correctly for both proportional (English) and
/// full-width (Korean/CJK) fonts.
///
/// Requires Unity 2020.1+ (ITextPreprocessor added in TMP 3.0).
/// </summary>
[RequireComponent(typeof(TMP_Text))]
public class RubyText : MonoBehaviour, ITextPreprocessor
{
    // Matches <ruby=ANNOTATION>BASE TEXT</ruby>
    private static readonly Regex RubyTagPattern =
        new(@"<ruby=([^>]+)>(.*?)<\/ruby>", RegexOptions.Singleline);

    [Header("Ruby Style")]
    [Range(0.3f, 0.7f)]
    [SerializeField] private float rubyScale = 0.5f;          // ruby font size relative to base
    [SerializeField] private float rubyVerticalOffset = 0.6f; // vertical lift in em units

    private TMP_Text _tmp;

    private void Awake()
    {
        _tmp = GetComponent<TMP_Text>();
        _tmp.textPreprocessor = this;
    }

    private void OnDestroy()
    {
        if (_tmp != null && _tmp.textPreprocessor == (ITextPreprocessor)this)
            _tmp.textPreprocessor = null;
    }

    // ── ITextPreprocessor ────────────────────────────────────────────────────

    /// <summary>
    /// Called automatically by TMP before layout.
    /// Converts all ruby tags to voffset/size/space markup using real glyph metrics.
    /// </summary>
    public string PreprocessText(string text)
    {
        if (string.IsNullOrEmpty(text)) return text;

        TMP_FontAsset font = _tmp != null ? _tmp.font : null;

        return RubyTagPattern.Replace(text, m =>
            BuildRubyMarkup(m.Groups[2].Value, m.Groups[1].Value,
                rubyScale, rubyVerticalOffset, font));
    }

    // ── Public static API ────────────────────────────────────────────────────

    /// <summary>
    /// Converts raw text with ruby tags to TMP markup.
    /// Pass a font asset for accurate English centering; omit for CJK-only use.
    /// </summary>
    public static string Process(string rawText,
        float rubyScale = 0.5f, float voffset = 0.6f, TMP_FontAsset font = null)
    {
        if (string.IsNullOrEmpty(rawText)) return string.Empty;

        return RubyTagPattern.Replace(rawText, m =>
            BuildRubyMarkup(m.Groups[2].Value, m.Groups[1].Value, rubyScale, voffset, font));
    }

    /// <summary>
    /// Builds TMP rich-text markup for one ruby pair.
    ///
    /// Strategy:
    ///   1. Measure both strings using font glyph metrics (falls back to char-count for CJK).
    ///   2. Output ruby with voffset — it floats up but still occupies horizontal space.
    ///   3. Rewind with a negative space back to the start position.
    ///   4. Output the base text, centered under the ruby.
    /// </summary>
    public static string BuildRubyMarkup(string baseText, string rubyText,
        float scale = 0.5f, float voffset = 0.6f, TMP_FontAsset font = null)
    {
        if (string.IsNullOrEmpty(baseText)) return string.Empty;
        if (string.IsNullOrEmpty(rubyText)) return baseText;

        // Measure widths in em using real glyph data when available
        float rubyWidthEm = MeasureWidthEm(rubyText, font, scale);
        float baseWidthEm = MeasureWidthEm(baseText, font, 1f);

        int    sizePercent = Mathf.RoundToInt(scale * 100f);
        string rubyMarkup  = $"<voffset={voffset}em><size={sizePercent}%>{rubyText}</size></voffset>";

        var sb = new StringBuilder();

        if (rubyWidthEm >= baseWidthEm)
        {
            // Ruby is wider → center base underneath
            float pad = (rubyWidthEm - baseWidthEm) * 0.5f;

            sb.Append(rubyMarkup);
            sb.Append(Space(-rubyWidthEm)); // rewind to start
            sb.Append(Space(pad));          // left-pad base
            sb.Append(baseText);
            sb.Append(Space(pad));          // right-pad base
        }
        else
        {
            // Base is wider → center ruby above
            float pad = (baseWidthEm - rubyWidthEm) * 0.5f;

            sb.Append(Space(pad));                      // shift ruby right to center it
            sb.Append(rubyMarkup);
            sb.Append(Space(-(rubyWidthEm + pad)));     // rewind to start of base
            sb.Append(baseText);
        }

        return sb.ToString();
    }

    // ── Width measurement ────────────────────────────────────────────────────

    /// <summary>
    /// Returns the width of a string in em units.
    ///
    /// With a font: reads each glyph's horizontalAdvance from the font asset.
    ///   horizontalAdvance / faceInfo.pointSize  =  width in em for that glyph.
    ///   This is accurate for both proportional (English) and full-width (Korean/CJK) fonts.
    ///
    /// Without a font: falls back to character count, which is accurate for
    ///   full-width glyphs (each char ≈ 1em) but approximate for Latin text.
    /// </summary>
    private static float MeasureWidthEm(string text, TMP_FontAsset font, float sizeMultiplier)
    {
        if (font == null)
            return text.Length * sizeMultiplier; // CJK fallback

        float total    = 0f;
        float pointSize = font.faceInfo.pointSize;

        foreach (char c in text)
        {
            if (font.characterLookupTable.TryGetValue(c, out TMP_Character ch))
                total += ch.glyph.metrics.horizontalAdvance / pointSize * sizeMultiplier;
            else
                total += 0.5f * sizeMultiplier; // glyph not in font (e.g. fallback font used)
        }

        return total;
    }

    private static string Space(float em) => $"<space={em:0.###}em>";
}
