﻿////////////////////////////////////////////////////////////////////////////////

using Markdig.Renderers;
using Markdig.Syntax;
using UnityEditor;

namespace MG.MDV
{
    ////////////////////////////////////////////////////////////////////////////////
    // <pre><code>...</code></pre>
    /// <see cref="Markdig.Renderers.Html.CodeBlockRenderer"/>

    public class RendererBlockCode : MarkdownObjectRenderer<RendererMarkdown, CodeBlock>
    {
        protected override void Write( RendererMarkdown renderer, CodeBlock block )
        {
            renderer.EnsureLine();

            // TODO: use monospace font / block ...
            renderer.WriteLeafRawLines( block );
            EditorGUILayout.TextArea( renderer.GetText() );
        }
    }
}
