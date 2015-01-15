using D2.FileTypes;
using SharpDX;
using SharpDX.Toolkit.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace D2.Game
{
    public class Area
    {
        private DS1File level;
        private List<DT1Texture> tiles;

        private float billboardWidth = (float)Math.Sqrt(5 * 5 * 2);
        private float billboardHeight = (float)Math.Sqrt(5 * 5 * 2) * 6.0f * 1.8f;

        public Area(DS1File level, List<DT1Texture> tiles)
        {
            this.level = level;
            this.tiles = tiles;
        }

        public void DrawFloors(GraphicsDevice GraphicsDevice, Matrix viewMatrix, Matrix projectionMatrix, Effect floorEffect, GeometricPrimitive plane)
        {
            GraphicsDevice.SetRasterizerState(GraphicsDevice.RasterizerStates.CullNone);
            GraphicsDevice.SetBlendState(GraphicsDevice.BlendStates.NonPremultiplied);

            Matrix worldMatrix;
            
            for (int wx = 0; wx < level.Width; wx++)
            {
                for (int wy = 0; wy < level.Height; wy++)
                {
                    worldMatrix = Matrix.Translation(wx * 5, wy * 5, 0);

                    Matrix worldView = Matrix.Multiply(worldMatrix, viewMatrix);
                    Matrix worldViewProj = Matrix.Multiply(worldView, projectionMatrix);
                    floorEffect.Parameters["WorldViewProj"].SetValue(worldViewProj);

                    foreach (var floor in level.floors)
                    {
                        var tile = floor[wx + (wy * level.Width)];

                        if (tile.prop1 != 0)
                        {

                            int main_index = (tile.prop3 >> 4) + ((tile.prop4 & 0x03) << 4);
                            int sub_index = tile.prop2;

                            int dt1idx = 0;
                            bool found = false;
                            for (int i = 0; i < tiles.Count; i++)
                            {
                                var dt1 = tiles[i].File;

                                if (dt1 != null)
                                {
                                    for (int j = 0; j < dt1.FloorHeaders.Count; j++)
                                    {
                                        var item = dt1.FloorHeaders[j];
                                        if ((item.tile.Orientation == 0) &&
                                           (item.tile.MainIndex == main_index) &&
                                           (item.tile.SubIndex == sub_index)
                                         )
                                        {
                                            floorEffect.Parameters["Texture"].SetResource(tiles[i].FloorsTexture);
                                            dt1idx = j;
                                            found = true;
                                            //break;
                                        }
                                    }
                                }

                                if (found) break;
                            }

                            int ti = dt1idx;

                            floorEffect.Parameters["TileIndex"].SetValue((float)ti);


                            floorEffect.Techniques[0].Passes[0].Apply();


                            plane.Draw(floorEffect);
                        }
                    }
                }
            }
        }

        public void DrawWalls(GraphicsDevice GraphicsDevice, Matrix viewMatrix, Matrix projectionMatrix, Effect floorEffect, GeometricPrimitive billboard)
        {
            Matrix worldMatrix;

            for (int wx = 0; wx < level.Width; wx++)
            {
                for (int wy = 0; wy < level.Height; wy++)
                {
                    worldMatrix = Matrix.RotationX(MathUtil.DegreesToRadians(-90)) * Matrix.RotationZ(MathUtil.DegreesToRadians(-45)) * Matrix.Translation((wx * 5) + (billboardWidth / 2.75f), (wy * 5) + (billboardWidth / 2.75f), billboardHeight / 2);

                    Matrix worldView = Matrix.Multiply(worldMatrix, viewMatrix);
                    Matrix worldViewProj = Matrix.Multiply(worldView, projectionMatrix);
                    floorEffect.Parameters["WorldViewProj"].SetValue(worldViewProj);

                    for (int wi = 0; wi < level.walls.Count; wi++)
                    {
                        var wall = level.walls[wi];

                        var tile = wall[wx + (wy * level.Width)];
                        var tileOrientation = level.orientations[wi][wx + (wy * level.Width)];

                        DrawWall(tile, tileOrientation.orientation, floorEffect, billboard);

                        if (tileOrientation.orientation == 3)
                        {
                            DrawWall(tile, 4, floorEffect, billboard);
                        }
                    }
                }
            }
        }

        private void DrawWall(D2.FileTypes.DS1File.CELL_W_S tile, int orientation, Effect floorEffect, GeometricPrimitive billboard)
        {    
            if (tile.prop1 != 0)
            {
                int main_index = (tile.prop3 >> 4) + ((tile.prop4 & 0x03) << 4);
                int sub_index = tile.prop2;

                int dt1idx = 0;
                bool found = false;
                for (int i = 0; i < tiles.Count; i++)
                {
                    var dt1 = tiles[i].File;

                    if (dt1 != null)
                    {
                        foreach (var item in dt1.WallHeaders
                            //.OrderBy(x => x.tile.Orientation)
                            //.ThenBy(x => x.tile.MainIndex)
                            //.ThenBy(x => x.tile.SubIndex)
                            //.ThenBy(x => x.tile.RarityFrameIndex)
                            //.ThenBy(x => x.FloorWallIndex)
                            )
                        {
                            if ((item.tile.Orientation == orientation) &&
                                (item.tile.MainIndex == main_index) &&
                                (item.tile.SubIndex == sub_index)
                                )
                            {
                                floorEffect.Parameters["Texture"].SetResource(tiles[i].WallsTexture);
                                dt1idx = item.FloorWallIndex;
                                found = true;
                                //break;
                            }

                            if (orientation == 18 || orientation == 19)
                            {
                                if (orientation == 18)
                                    orientation = 19;
                                else
                                    orientation = 18;

                                if ((item.tile.Orientation == orientation) && (item.tile.MainIndex == main_index) && (item.tile.SubIndex == sub_index))
                                {
                                    floorEffect.Parameters["Texture"].SetResource(tiles[i].WallsTexture);
                                    dt1idx = item.FloorWallIndex;
                                    found = true;
                                    //break;
                                }

                                sub_index = 0;
                                if ((item.tile.Orientation == orientation) && (item.tile.MainIndex == main_index) && (item.tile.SubIndex == sub_index))
                                {
                                    floorEffect.Parameters["Texture"].SetResource(tiles[i].WallsTexture);
                                    dt1idx = item.FloorWallIndex;
                                    found = true;
                                    //break;
                                }
                            }
                        }
                    }

                    if (found) break;
                }

                int ti = dt1idx;

                floorEffect.Parameters["TileIndex"].SetValue((float)ti);


                floorEffect.Techniques[0].Passes[0].Apply();


                billboard.Draw(floorEffect);
            }
        }
    }
}
