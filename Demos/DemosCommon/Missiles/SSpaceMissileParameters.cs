﻿using System;
using System.Drawing; // RectangleF
using OpenTK;
using OpenTK.Graphics;

namespace SimpleScene.Demos
{
    // TODO: fuel strategy???

    public class SSpaceMissileParameters
    {
        public delegate Matrix4 SpawnTxfmDelegate(ISSpaceMissileTarget target, Vector3 launcherPos, Vector3 launcherVel);
        public delegate ISSpaceMissileDriver 
            EjectionCreationDelegate(SSpaceMissileData missile, Vector3 clusterPos, Vector3 clusterVel);
        public delegate ISSpaceMissileDriver PursuitCreationDelegate(SSpaceMissileData missile);
        public delegate void MissileEventDelegate (Vector3 position, SSpaceMissileParameters mParams);

        #region simulation parameters
        public float simulationStep = 0.05f;
        #endregion

        #region ejection
        public float ejectionVelocity = 10f;
        public float ejectionAcc = 4f;
        //public float ejectionAcc = 0.2f;
        public float ejectionMaxRotationVel = 5f;

        public BodiesFieldGenerator spawnGenerator 
            = new BodiesFieldGenerator(new ParticlesSphereGenerator(Vector3.Zero, 1f));
        public SpawnTxfmDelegate spawnTxfm 
            = (target, launcherPos, launcherVel) => { return Matrix4.CreateTranslation(launcherPos); };
        public float spawnDistanceScale = 10f;

        public EjectionCreationDelegate createEjection = (missile, clusterPos, clusterVel) =>
            { return new SSimpleMissileEjectionDriver (missile, clusterPos, clusterVel); };
        #endregion

        #region pursuit
        /// <summary> time after launch when we transition from ejection into pursuit phase </summary>
        public float activationTime = 0.5f;
        public PursuitCreationDelegate createPursuit = (missile) => 
            { return new SProportionalNavigationPursuitDriver (missile); };
        /// <summary> basic proportional navigation's coefficient (N) </summary>
        public float pursuitNavigationGain = 3f;
        /// <summary> augment proportional navigation with target's lateral acceleration. needs more testing </summary>
        public bool pursuitAugmentedPN = false;
        /// <summary> throttles/accelerates the missile hit at the time specified by the hit time </summary>
        public bool pursuitHitTimeCorrection = false;
        /// <summary> ignored when hit time correction is active. can be set to pos. infinity </summary>
        public float pursuitMaxVelocity = float.PositiveInfinity;
        /// <summary> ignored when hit time correction is active </summary>
        public float pursuitMaxAcc = 10f;
        #endregion

        #region at target
        public float atTargetDistance = 1f;
        public bool terminateWhenAtTarget = true;
        public MissileEventDelegate targetHitHandlers = null;
        #endregion

        #region body render parameters
        /// <summary> radians rate by which the missile's visual orientation leans into its velocity </summary>
        public float pursuitVisualRotationRate = 0.1f;
        /// <summary> Missile mesh must be facing into positive Z axis </summary>
        public SSAbstractMesh missileMesh
            = SSAssetManager.GetInstance<SSMesh_wfOBJ>("missiles", "missile.obj");
        public float missileBodyScale = 0.3f;
        #endregion

        #region smoke render parameters
        public SSTexture smokeParticlesTexture
            = SSAssetManager.GetInstance<SSTextureWithAlpha>("explosions", "fig7.png");
        public RectangleF[] smokeSpriteRects = {
            new RectangleF(0f,    0f,    0.25f, 0.25f),
            new RectangleF(0f,    0.25f, 0.25f, 0.25f),
            new RectangleF(0.25f, 0.25f, 0.25f, 0.25f),
            new RectangleF(0.25f, 0f,    0.25f, 0.25f),
        };
        public Color4 smokeColor = Color4.LightGray;
        public float smokeEmissionFrequencyMin = 10f;
        public float smokeEmissionFrequencyMax = 200f;
        public int smokePerEmissionMin = 1;
        public int smokePerEmissionMax = 1;

        public float ejectionSmokeSizeMin = 2f;
        public float ejectionSmokeSizeMax = 15f;           
        public float ejectionSmokeDuration = 1f;

        /// <summary> distance from the center of the mesh to the jet (before scale) </summary>
        public Color4 innerFlameColor = Color4.LightGoldenrodYellow;
        public Color4 outerFlameColor = Color4.DarkOrange;
        public float jetPosition = 4.2f;
        public float flameSmokeSizeMin = 2f;
        public float flameSmokeSizeMax = 3f;
        public float flameSmokeDuration = 0.5f;
        #endregion

        public bool debuggingAid = false;
    }
}

