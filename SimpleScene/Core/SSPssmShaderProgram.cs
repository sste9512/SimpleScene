// Copyright(C) David W. Jeske, 2014, All Rights Reserved.
// Released to the public domain. 

using System;

using OpenTK;
using OpenTK.Graphics.OpenGL;
using SimpleScene;
using System.Drawing;
using System.Collections.Generic;

namespace SimpleScene
{
	public interface ISSPssmShaderProgram
	{
		void Activate();

		Matrix4 UniObjectWorldTransform { set; }
		Matrix4[] UniShadowMapVPs { set; }
	}

	public class SSPssmShaderProgram : SSShaderProgram, ISSPssmShaderProgram
    {
        private static string c_ctx = "./Shaders/Shadowmap";

        #region Shaders
        private readonly SSShader m_vertexShader;  
        private readonly SSShader m_fragmentShader;
        private readonly SSShader m_geometryShader;
        #endregion

        #region Uniform Locations
        private readonly int u_numShadowMaps;
        private readonly int u_objectWorldTransform;
        private readonly int u_shadowMapVPs;
        #endregion

        #region Uniform Modifiers
        public Matrix4 UniObjectWorldTransform {
			set { GL.ProgramUniformMatrix4 (m_programID, u_objectWorldTransform, 1, false, ref value.Row0.X); }
        }

		public Matrix4[] UniShadowMapVPs {
			set { GL.ProgramUniformMatrix4 (m_programID, u_shadowMapVPs, value.Length, false, ref value [0].Row0.X); }
		}
        #endregion

		public SSPssmShaderProgram(string preprocessorDefs = null)
        {
			string glExtStr = GL.GetString (StringName.Extensions).ToLower ();
			if (!glExtStr.Contains ("gl_ext_gpu_shader4")) {
				Console.WriteLine ("PSSM shader not supported");
				m_isValid = false;
				return;
			}
            m_vertexShader = SSAssetManager.GetInstance<SSVertexShader>(c_ctx, "pssm_vertex.glsl");
			m_vertexShader.Prepend (preprocessorDefs);
            m_vertexShader.LoadShader();
            attach(m_vertexShader);

            m_fragmentShader = SSAssetManager.GetInstance<SSFragmentShader>(c_ctx, "pssm_fragment.glsl");
			m_fragmentShader.Prepend (preprocessorDefs);
            m_fragmentShader.LoadShader();
            attach(m_fragmentShader);

            m_geometryShader = SSAssetManager.GetInstance<SSGeometryShader>(c_ctx, "pssm_geometry.glsl");
			m_geometryShader.Prepend (preprocessorDefs);
            m_geometryShader.LoadShader();
            GL.Ext.ProgramParameter (m_programID, ExtGeometryShader4.GeometryInputTypeExt, (int)All.Triangles);
            GL.Ext.ProgramParameter (m_programID, ExtGeometryShader4.GeometryOutputTypeExt, (int)All.TriangleStrip);
            GL.Ext.ProgramParameter (m_programID, ExtGeometryShader4.GeometryVerticesOutExt, 3 * SSParallelSplitShadowMap.c_numberOfSplits);
            attach(m_geometryShader);
            link();
            Activate();

 			u_shadowMapVPs = getUniLoc ("shadowMapVPs");
            u_objectWorldTransform = getUniLoc("objWorldTransform");
            u_numShadowMaps = getUniLoc("numShadowMaps");

            GL.Uniform1(u_numShadowMaps, SSParallelSplitShadowMap.c_numberOfSplits);

			m_isValid = checkGlValid();
        }
    }
}