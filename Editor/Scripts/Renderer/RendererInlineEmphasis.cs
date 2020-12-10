////////////////////////////////////////////////////////////////////////////////

using Markdig.Renderers;
using Markdig.Syntax.Inlines;

namespace MG.MDV
{
    ////////////////////////////////////////////////////////////////////////////////
    // <b><i>...</i></b>
    /// <see cref="Markdig.Renderers.Html.Inlines.EmphasisInlineRenderer"/>

    public class RendererInlineEmphasis : MarkdownObjectRenderer<RendererMarkdown, EmphasisInline>
    {
        protected override void Write( RendererMarkdown renderer, EmphasisInline node )
        {
            bool prev = false;
            
            #pragma warning disable 618
            if( node.IsDouble )
            {
                prev = renderer.Style.Bold;
                renderer.Style.Bold = true;
            }
            else
            {
                prev = renderer.Style.Italic;
                renderer.Style.Italic = true;
            }

            renderer.WriteChildren( node );

            if( node.IsDouble )
            {
                renderer.Style.Bold = prev;
            }
            else
            {
                renderer.Style.Italic = prev;
            }
            #pragma warning restore 618
        }
    }
}

