using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using AngleSharp;
using AngleSharp.Css.Dom;
using AngleSharp.Css.Parser;
using AngleSharp.Dom;
using AngleSharp.Html.Dom;
using Fizzler.Systems.HtmlAgilityPack;
using HtmlAgilityPack;
using Common.ExtensionMethods;

namespace Common.Utility
{
    public static class HtmlUtility
    {
        public static readonly string DefaultWebFont = "Calibri";

        private static readonly List<string> StandardWebFonts = new()
        {
            "Arial",
            "Arial Black",
            "Calibri",
            "Courier New",
            "Garamond",
            "Georgia",
            "Helvetica",
            "Lucida Console",
            "Monaco",
            "Times New Roman",
            "Trebuchet MS",
            "Verdana"
        };

        public static HtmlDocument GetFullyFormedHtmlDocumentFromHtml(string html)
        {
            var htmlDocument = new HtmlDocument();
            htmlDocument.LoadHtml(html);

            if (htmlDocument.DocumentNode.QuerySelector("body") == null)
            {
                html = $"<body>{html}</body>";
            }

            if (htmlDocument.DocumentNode.QuerySelector("head") == null)
            {
                html = $"<head></head>{html}";
            }

            if (htmlDocument.DocumentNode.QuerySelector("html") == null)
            {
                html = $"<html>{html}</html>";
            }

            htmlDocument.LoadHtml(html);

            return htmlDocument;
        }

        public static void TrimEmptyTrailingContentFromHtmlDocument(HtmlDocument htmlDocument)
        {
            static bool IsLineBreakEmptyParagraphOrTextNode(HtmlNode node)
            {
                if (node == null)
                {
                    return false;
                }

                return node.Name == "br"
                    || ((node.Name == "p" || node.Name == "#text")
                        && string.IsNullOrWhiteSpace(node.InnerText.Replace("\r", string.Empty).Replace("\n", string.Empty).Replace("\t", string.Empty).Trim()));
            }

            static bool RemoveFinalLineBreakNode(HtmlNodeCollection bodyOrTopLevelChildNodes)
            {
                if (bodyOrTopLevelChildNodes.Any())
                {
                    var lastNode = bodyOrTopLevelChildNodes.Last();

                    if (IsLineBreakEmptyParagraphOrTextNode(lastNode.ChildNodes.LastOrDefault()) == true)
                    {
                        lastNode.ChildNodes.Remove(lastNode.ChildNodes.Last());
                        return true;
                    }
                    else if (IsLineBreakEmptyParagraphOrTextNode(lastNode))
                    {
                        bodyOrTopLevelChildNodes.Remove(lastNode);
                        return true;
                    }
                }

                return false;
            }

            var maxIterations = 1000; // prevent infinite loop, just in case
            var iteration = 0;

            var bodyOrTopLevelNode = htmlDocument.DocumentNode.QuerySelector("body") ?? htmlDocument.DocumentNode;

            while (RemoveFinalLineBreakNode(bodyOrTopLevelNode.ChildNodes) && iteration < maxIterations)
            {
                iteration++;
            }

            if (iteration >= maxIterations)
            {
                throw new InvalidOperationException("Infinite loop whilst attempting to sanitise template HTML");
            }
        }

        public static bool IsStandardWebFont(string fontFamily) => StandardWebFonts.Contains(fontFamily, StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Parses the HTML, ensures that all elements specify at least one default font family to avoid things displaying in Times New Roman fallback on the FE.
        /// Also parses out the most commonly found font family and size.
        /// </summary>
        // SW 2024.02.02 - I've added AngleSharp to the project for this since it has proper support for parsing style declarations
        // ideally, we should use this everywhere in preference of HtmlAgilityPack to avoid including both
        // however no time for that now
        public static async Task<(string AdjustedHtml, string MostCommonFontFamily, string MostCommonFontSize)> EnsureDefaultWebFontIsAppliedAndGetMostCommonFontPropertiesFromHtmlAsync(string html)
        {
            var context = BrowsingContext.New(Configuration.Default.WithDefaultLoader().WithRenderDevice());
            var htmlDocument = await context.OpenAsync(req => req.Content(html));

            var bodyDescendantTextContainingElements = htmlDocument.DocumentElement.QuerySelector("body").Descendants()
                .OfType<IHtmlElement>()
                .Where(x => x is IHtmlParagraphElement || x is IHtmlSpanElement)
                .ToList();

            var cssParser = new CssParser();
            var allFontFamliesFound = new List<string>();
            var allFontSizesFound = new List<string>();

            foreach (var textContainingElement in bodyDescendantTextContainingElements)
            {
                var inlineStyle = cssParser.ParseDeclaration(textContainingElement.GetAttribute("style"));
                ICssStyleDeclaration computedStyle = null;

                // sometimes .GetStyle() will return null, probably malformed content in some way
                // if it does, it means GetComputedStyle will throw a NRE, so we need to defensively code around this
                if (textContainingElement.GetStyle() != null)
                {
                    computedStyle = htmlDocument.DefaultView.GetComputedStyle(textContainingElement);
                }

                // inline declaration takes precedence over computed
                var computedOrDeclaredFontFamily = inlineStyle.GetPropertyValue("font-family") ?? computedStyle?.GetPropertyValue("font-family");
                var computedOrDeclaredFontSize = inlineStyle.GetPropertyValue("font-size") ?? computedStyle?.GetPropertyValue("font-size");

                var familiesCleaned = string.IsNullOrWhiteSpace(computedOrDeclaredFontFamily)
                    ? new List<string>()
                    : computedOrDeclaredFontFamily.Split(",").Select(x => x.Trim().Trim('"').Trim('\'')).ToList();

                allFontFamliesFound.AddRange(familiesCleaned);

                // does this element contain any non-whitespace text directly?
                // similar to the above, if GetStyle() returns null, this won't work, so we will just use a default of false
                var containsOwnText = false;

                if (textContainingElement.GetStyle() != null)
                {
                    var elementText = textContainingElement.GetInnerText().RemoveAllWhitespaceCharacters();
                    var elementChildrenText = textContainingElement.Children.Select(x => x.GetInnerText()).StringJoin(string.Empty).RemoveAllWhitespaceCharacters();
                    containsOwnText = elementText != elementChildrenText;
                }

                if (containsOwnText)
                {
                    // ensure we always have a standard web font in the list
                    if (!familiesCleaned.Any(IsStandardWebFont))
                    {
                        var inlineStyleFontFamily = inlineStyle.GetPropertyValue("font-family");

                        if (!string.IsNullOrWhiteSpace(inlineStyleFontFamily))
                        {
                            inlineStyle.SetProperty(
                                "font-family",
                                inlineStyleFontFamily.Split(",").Select(x => x.Trim()).Concat(new[] { DefaultWebFont }).StringJoin(", "));
                        }
                        else
                        {
                            inlineStyle.SetProperty(
                                "font-family",
                                DefaultWebFont);
                        }

                        textContainingElement.SetStyle(inlineStyle.CssText);
                    }
                }

                if (!string.IsNullOrWhiteSpace(computedOrDeclaredFontSize))
                {
                    allFontSizesFound.Add(computedOrDeclaredFontSize);
                }
            }

            var mostCommonFontFamily = allFontFamliesFound
                .GroupBy(x => x, StringComparer.OrdinalIgnoreCase)
                .MaxBy(x => x.Count())?
                .Key;

            var mostCommonFontSize = allFontSizesFound
                .GroupBy(x => x, StringComparer.OrdinalIgnoreCase)
                .MaxBy(x => x.Count())?
                .Key;

            return (htmlDocument.DocumentElement.OuterHtml, mostCommonFontFamily, mostCommonFontSize);
        }
    }
}
