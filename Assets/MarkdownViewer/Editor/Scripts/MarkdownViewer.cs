﻿using Markdig;
using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using Markdig.Extensions.JiraLinks;
using System.Collections.Generic;

namespace MG.MDV
{
    public class MarkdownViewer
    {
        public bool     IsMarkdown  { get; private set; }
        public string   Text        { get; private set; }

        private GUISkin         mSkin            = null;
        private string          mCurrentPath     = string.Empty;
        private HandlerImages   mHandlerImages   = new HandlerImages();
        private HandlerNavigate mHandlerNavigate = new HandlerNavigate();

        private Vector2         mScrollPos       = Vector2.zero;
        private Layout          mLayout          = null;
        private bool            mRaw             = false;

        private static List<string> mExtensions = new List<string> { ".md", ".markdown" };
        private static History      mHistory    = new History();

        public MarkdownViewer( GUISkin skin, string path, string content )
        {
            mSkin        = skin;
            mCurrentPath = path;
            Text         = content;

            Setup( path, content );
        }


        //------------------------------------------------------------------------------

        public bool Update()
        {
            return mHandlerImages.UpdateRequests();
        }

        //------------------------------------------------------------------------------

        void Setup( string path, string contents )
        {
            Text         = contents;
            mCurrentPath = path;

            var ext = Path.GetExtension( mCurrentPath ).ToLower();
            IsMarkdown = mExtensions.Contains( ext );

            if( !IsMarkdown )
            {
                return;
            }

            mLayout = ParseDocument( mCurrentPath );

            mHandlerImages.CurrentPath = mCurrentPath;

            mHandlerNavigate.CurrentPath = mCurrentPath;
            mHandlerNavigate.History     = mHistory;
            mHandlerNavigate.FindBlock   = ( id ) => mLayout.Find( id );
            mHandlerNavigate.ScrollTo    = ( pos ) => mScrollPos.y = pos;
        }

        //------------------------------------------------------------------------------

        Layout ParseDocument( string filename )
        {
            mHistory.OnOpen( filename );

            var context  = new Context( mSkin, mHandlerImages, mHandlerNavigate );
            var builder  = new LayoutBuilder( context );
            var renderer = new RendererMarkdown( builder );

            var pipelineBuilder = new MarkdownPipelineBuilder()
                .UseAutoLinks()
            ;

            if( !string.IsNullOrEmpty( Preferences.JIRA ) )
            {
                pipelineBuilder.UseJiraLinks( new JiraLinkOptions( Preferences.JIRA ) );
            }

            var pipeline = pipelineBuilder.Build();
            pipeline.Setup( renderer );

            var doc = Markdown.Parse( Text, pipeline );
            renderer.Render( doc );

            return builder.GetLayout();
        }


        //------------------------------------------------------------------------------

        protected Rect GetEditorRect()
        {
#if UNITY_2019
            var height = Screen.height - 138.0f; // TODO: need better way of getting height!
            GUILayout.Space( height );
            return new Rect( 0.0f, 4.0f, Screen.width, height );
#else
            GUILayout.FlexibleSpace();
            var rectContainer = GUILayoutUtility.GetLastRect();
            rectContainer.width = Screen.width;

            return rectContainer;
#endif
        }

        //------------------------------------------------------------------------------

        public void Draw()
        {
            GUI.skin = mSkin;
            GUI.enabled = true;

            // content rect

            var rectContainer = GetEditorRect();


            // clear background

            var rectFullScreen = new Rect( 0.0f, rectContainer.yMin - 4.0f, Screen.width, Screen.height );
            GUI.DrawTexture( rectFullScreen, EditorGUIUtility.whiteTexture, ScaleMode.StretchToFill, false );


            // scroll window

            var padLeft     = 8.0f;
            var padRight    = 4.0f;
            var padHoriz    = padLeft + padRight;
            var scrollWidth = GUI.skin.verticalScrollbar.fixedWidth;
            var minWidth    = rectContainer.width - scrollWidth - padHoriz;
            var maxHeight   = ContentHeight( minWidth );

            var hasScrollbar =  maxHeight >= rectContainer.height;
            var contentWidth = hasScrollbar ? minWidth : rectContainer.width - padHoriz;
            var rectContent  = new Rect( -padLeft, 0.0f, contentWidth, maxHeight );

            // draw content

            using( var scroll = new GUI.ScrollViewScope( rectContainer, mScrollPos, rectContent ) )
            {
                mScrollPos = scroll.scrollPosition;

                if( mRaw )
                {
                    rectContent.width = minWidth - GUI.skin.button.fixedWidth;
                    DrawRaw( rectContent );
                }
                else
                {
                    DrawMarkdown( rectContent );
                }
            }

            DrawToolbar( rectContainer, hasScrollbar ? scrollWidth + padRight : padRight );
        }

        //------------------------------------------------------------------------------

        float ContentHeight( float width )
        {
            return mRaw ? GUI.skin.GetStyle( "raw" ).CalcHeight( new GUIContent( Text ), width ) : mLayout.Height;
        }

        //------------------------------------------------------------------------------

        void DrawToolbar( Rect rect, float marginRight )
        {
            var style  = GUI.skin.button;
            var size   = style.fixedHeight;
            var btn    = new Rect( rect.xMax - size - marginRight, rect.yMin, size, size );

            if( GUI.Button( btn, string.Empty, GUI.skin.GetStyle( mRaw ? "btnRaw" : "btnFile" ) ) )
            {
                mRaw = !mRaw;
            }

            if( mRaw == false )
            {
                if( mHistory.CanForward )
                {
                    btn.x -= size;

                    if( GUI.Button( btn, string.Empty, GUI.skin.GetStyle( "btnForward" ) ) )
                    {
                        Selection.activeObject = AssetDatabase.LoadAssetAtPath<TextAsset>( mHistory.Forward() );
                    }
                }

                if( mHistory.CanBack )
                {
                    btn.x -= size;

                    if( GUI.Button( btn, string.Empty, GUI.skin.GetStyle( "btnBack" ) ) )
                    {
                        Selection.activeObject = AssetDatabase.LoadAssetAtPath<TextAsset>( mHistory.Back() );
                    }
                }
            }
        }

        //------------------------------------------------------------------------------

        void DrawRaw( Rect rect )
        {
            EditorGUI.SelectableLabel( rect, Text, GUI.skin.GetStyle( "raw" ) );
        }

        //------------------------------------------------------------------------------

        void DrawMarkdown( Rect rect )
        {
            switch( Event.current.type )
            {
                case EventType.Ignore:
                    break;

                case EventType.ContextClick:
                    var menu = new GenericMenu();
                    menu.AddItem( new GUIContent( "View Source" ), false, () => mRaw = !mRaw );
                    menu.ShowAsContext();

                    break;

                case EventType.Layout:
                    mLayout.Arrange( rect.width );
                    break;

                default:
                    mLayout.Draw();
                    break;
            }
        }
    }
}
