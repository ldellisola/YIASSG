using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MDParser.Utils
{
    public static class TreeNodeExtension
    {
        public static void WriteMD(this TitleNode node, StringBuilder bld, int index)
        {
            for (int i = 0; i < node.Level; i++)
                bld.Append('\t');

            if (node.Level == 0)
            {
                bld.AppendLine($"{index}. [{node.Text}]({node.FileName})");
            }
            else
            {
                bld.Append($"- [{node.Text}]({node.FileName}");
                for (int i = 0; i < node.RealLevel; i++)
                    bld.Append('#');
                bld.AppendLine($"{node.Text})");
            }
           


            node.ChildNodes.ForEach(t=>t.WriteMD(bld,index));
        }

        public static void RearrangeTree(this TitleNode node, int index = 0)
        {
            node.Level = index;

            node.ChildNodes.ForEach(t=>t.RearrangeTree(index+1));
        }
    }
}
