using System.Text;
using System.Text.Encodings.Web;
using System.Text.RegularExpressions;

namespace DChat.Services
{
    public static partial class MessageEncoder
    {
        [GeneratedRegex(
            @"^(http|https|ftp)\://[a-zA-Z0-9\-\.]+\.[a-zA-Z]{2,3}(:[a-zA-Z0-9]*)?/?([a-zA-Z0-9\-\._\?\,\'/\\\+&amp;%\$#\=~])*$",
            RegexOptions.IgnoreCase | RegexOptions.Compiled)]
        private static partial Regex UrlRegex();

        public static string ToHtml(string text)
        {
            var span = text.AsSpan();
            var html = new StringBuilder(text.Length + 13); //13 is the length of "<span></span>"

            while (span.Length > 0)
            {
                var lineEnd = span.IndexOf('\n');
                if (lineEnd == -1)
                    lineEnd = span.Length;

                var line = span[..lineEnd];
                var start = 0;
                var newLine = true;

                foreach (var match in UrlRegex().EnumerateMatches(line))
                {
                    AppendSpan(html, line[start..match.Index], newLine);
                    newLine = false;
                    AppendUrl(html, line.Slice(match.Index, match.Length));
                    start = match.Index + match.Length;
                }

                AppendSpan(html, line[start..], newLine);

                if (lineEnd < span.Length)
                    lineEnd++; //skip '\n'

                span = span[lineEnd..];
            }

            return html.ToString();
        }

        private static void AppendSpan(StringBuilder html, ReadOnlySpan<char> text, bool newLine)
        {
            while (text.Length > 0 && char.IsWhiteSpace(text[0]))
                text = text[1..];

            while (text.Length > 0 && char.IsWhiteSpace(text[^1]))
                text = text[..^1];

            if (newLine && html.Length > 0)
                html.Append("<br />");
            
            if (text.Length == 0)
                return;

            html.Append("<span>");
            AppendEncoded(html, text);
            html.Append("</span>");
        }

        private static void AppendEncoded(StringBuilder html, ReadOnlySpan<char> text)
        {
            Span<char> buf = stackalloc char[Math.Max(text.Length, 256)];

            while (text.Length > 0)
            {
                HtmlEncoder.Default.Encode(text, buf, out var consumed, out var written);
                html.Append(buf[..written]);
                text = text[consumed..];
            }
        }

        private static void AppendUrl(StringBuilder html, ReadOnlySpan<char> url)
        {
            html.Append("<a href=\"");
            html.Append(url);
            html.Append("\" target=\"_blank\" rel=\"noopener noreferrer\">");
            AppendEncoded(html, url);
            html.Append("</a>");
        }
    }
}
