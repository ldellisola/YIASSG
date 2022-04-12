using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Markdig.Parsers;
using Markdig.Syntax;

namespace YIASSG;

public class MarkdownDocument
{
    private StringBuilder _document;

    public MarkdownDocument(string document = "")
    {
        _document = new StringBuilder(document);
    }

    public string Build()
    {
        return _document.ToString();
    }

    public override string ToString()
    {
        return _document.ToString();
    }

    public MarkdownDocument AddIndentation(int levelOfIndentation = 1)
    {
        _document.AppendJoin("", Enumerable.Repeat('\t', levelOfIndentation));
        // _document.AppendLine();
        return this;
    }

    public MarkdownDocument AddNewLine(int linesCount = 1)
    {
        foreach (var i in Enumerable.Range(0, linesCount)) _document.AppendLine();

        return this;
    }

    public MarkdownDocument AddLine(string text)
    {
        AddText(text);
        AddNewLine();
        return this;
    }

    public MarkdownDocument AddOrderedList(IEnumerable<string> list, int levelOfIndentation = 0)
    {
        var order = 1;
        foreach (var line in list) AddOrderedListElement(order, levelOfIndentation);

        return this;
    }

    public MarkdownDocument AddOrderedListElement(int order, int levelOfIndentation = 0)
    {
        AddIndentation(levelOfIndentation);
        _document.Append($"{order}. ");
        return this;
    }


    public MarkdownDocument AddText(string text)
    {
        _document.Append(text);
        return this;
    }

    public MarkdownDocument AddHeading(int headingLevel = 1)
    {
        _document
            .AppendJoin("", Enumerable.Repeat('#', headingLevel))
            .Append(' ');
        return this;
    }

    public MarkdownDocument AddLink(string title, string href, int titleDepth = 0, string fragment = "")
    {
        _document.Append($"[{title}]({href}");
        _document.AppendJoin("", Enumerable.Repeat('#', titleDepth));
        _document.Append(fragment);
        _document.Append(")");
        return this;
    }

    public MarkdownDocument AddUnorderedList(IEnumerable<string> list, int levelOfIndentation = 0)
    {
        foreach (var line in list)
        {
            AddUnorderedListElement(levelOfIndentation);
            AddLine(line);
        }

        return this;
    }

    public MarkdownDocument AddUnorderedListElement(int levelOfIndentation = 0)
    {
        AddIndentation(levelOfIndentation);
        _document.Append($"- ");

        return this;
    }
}