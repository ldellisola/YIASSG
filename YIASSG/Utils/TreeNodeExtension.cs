using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading.Tasks;

namespace YIASSG.Utils;

public static class TreeNodeExtension
{
    public static void WriteMarkDown(this TitleNode node, MarkdownDocument doc, int index)
    {
        doc.AddUnorderedListElement(node.Level)
            .AddLink(node.Text, node.FileName, node.RealLevel, node.Text)
            .AddNewLine();


        node.ChildNodes.ForEach(t => t.WriteMarkDown(doc, index));
    }

    public static void RearrangeTree(this TitleNode node, int index = 0)
    {
        node.Level = index;

        node.ChildNodes.ForEach(t => t.RearrangeTree(index + 1));
    }
}