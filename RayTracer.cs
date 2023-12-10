using System;
using System.Runtime.InteropServices;

namespace testVR
{
    class RayTracer
    {
        private Geometry[] geometries;
        private Light[] lights;

        public RayTracer(Geometry[] geometries, Light[] lights)
        {
            this.geometries = geometries;
            this.lights = lights;
        }

        private double ImageToViewPlane(int n, int imgSize, double viewPlaneSize)
        {
            var u = -n * viewPlaneSize / imgSize;
            u += viewPlaneSize / 2;
            return u;
        }

        private Intersection FindFirstIntersection(Line ray, double minDist, double maxDist)
        {
            var intersection = Intersection.NONE;

            foreach (var geometry in geometries)
            {
                var intr = geometry.GetIntersection(ray, minDist, maxDist);

                if (!intr.Valid || !intr.Visible) continue;

                if (!intersection.Valid || !intersection.Visible)
                {
                    intersection = intr;
                }
                else if (intr.T < intersection.T)
                {
                    intersection = intr;
                }
            }

            return intersection;
        }

        private bool IsLit(Vector point, Light light, Ellipsoid ellipsoid)
        {
            // ADD CODE HERE: Detect whether the given point has a clear line of sight to the given light
            Line line = new Line(point, light.Position);
            foreach (var geometry in geometries)
            {
                if (!(geometry is RawCtMask))
                {
                    Ellipsoid ellipsoid2 = (Ellipsoid)geometry;
                    if ((ellipsoid2.Center - ellipsoid.Center).Length() < 0.001)    // disregard sphere on point
                    {
                        continue;
                    }

                    Intersection intersection = ellipsoid2.GetIntersection(line, 0, 1000);  // get other spheres from intersection
                    if (intersection.T > 0)
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        private double PlusX(double x)
        {
            if (x < 0)
                return 0;
            return x;
        }

        public void Render(Camera camera, int width, int height, string filename)
        {
            var background = new Color(0.2, 0.2, 0.2, 1.0);
            var viewParallel = (camera.Up ^ camera.Direction).Normalize();

            var image = new Image(width, height);

            var vecW = camera.Direction * camera.ViewPlaneDistance;
            
            for (var i = 0; i < width; i++)
            {
                for (var j = 0; j < height; j++)
                {
                    var dx = vecW + viewParallel * ImageToViewPlane(i, width, camera.ViewPlaneWidth) +
                             camera.Up * ImageToViewPlane(j, height, camera.ViewPlaneHeight);
                    dx.Normalize();

                    var line = new Line();
                    line.X0 = camera.Position;
                    line.Dx = dx;

                    var inters = FindFirstIntersection(line, camera.FrontPlaneDistance, camera.BackPlaneDistance);
                    if (inters.Valid)
                    {
                        Geometry geometry = inters.Geometry;
                        if (geometry is RawCtMask)
                        {
                            var color = new Color();
                            foreach (var light in lights)
                            {
                                var Vert = inters.Position;
                                var Nml = inters.Normal;
                                var Lgt = light.Position;
                                var Tr = (Lgt - Vert).Normalize();
                                var Rd = (Nml * (Nml * Tr) * 2 - Tr).Normalize();
                                var E = (camera.Position - Vert).Normalize();

                                color += inters.Material.Ambient * light.Ambient +
                                         inters.Material.Diffuse * light.Diffuse * PosX(Nml * Tr) +
                                         inters.Material.Specular * light.Specular *
                                         Math.Pow(PosX(E * Rd), inters.Material.Shininess);
                            }
                            image.SetPixel(i, j, color);
                        }
                        else
                        {
                            var color = new Color();
                            foreach (var light in lights)
                            {
                                var Vert = inters.Position;
                                var Nml = inters.Normal;
                                var Lgt = light.Position;
                                var Tr = (Lgt - Vert).Normalize();
                                var Rd = (Nml * (Nml * Tr) * 2 - Tr).Normalize();
                                var E = (camera.Position - Vert).Normalize();

                                if (IsLit(Vert, light, (Ellipsoid)geometry))
                                {
                                    color += geometry.Material.Ambient * light.Ambient +
                                             geometry.Material.Diffuse * light.Diffuse * PosX(Nml * Tr) +
                                             geometry.Material.Specular * light.Specular *
                                             Math.Pow(PosX(E * Rd), geometry.Material.Shininess);
                                }
                                else
                                {
                                    color += geometry.Material.Ambient * light.Ambient;
                                }
                            }
                            image.SetPixel(i, j, color);
                        }
                    }
                    else
                    {
                        image.SetPixel(i, j, background);
                    }
                }
            }

            image.Store(filename);
        }
    }
}