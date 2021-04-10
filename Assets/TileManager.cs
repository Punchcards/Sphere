using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using CoordinateSharp;
using System.Collections.Generic;
using UnityEngine.Networking;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine.UI;

public class TileManager : MonoBehaviour
{
    public GameObject playerPrefab;
    public GameObject playerObject;
    Coordinate playerPosition;
    ECEF rootTileECEF;
    public GameObject rootTile;

    public GameObject uiTextObject;
    private UnityEngine.UI.Text uiText;

    string url = @"https://www.google.co.uk/maps/dir/";

    private Dictionary<KeyValuePair<int, int>, GameObject> tiles = new Dictionary<KeyValuePair<int, int>, GameObject>();

    int currentTileX;
    int currentTileY;

    int zoom = 19;

    float rotationSpeed = 2.0f;
    float walkSpeed = 2.0f;
    float realisticWalkingSpeed = 1.49905207001f;
    float devWalkingSpeed = 20f;

    float heading = 0;

    double scale = 1000;

    public GameObject lowerPoint;
    public GameObject upperPoint;

    public GameObject arrowPrefab;
    public GameObject arrowObject;

    // Start is called before the first frame update
    async void Start()
    {
        lowerPoint = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        upperPoint = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        arrowObject = (GameObject)Instantiate(arrowPrefab);

        lowerPoint.transform.localScale *= 0.25f;
        upperPoint.transform.localScale *= 0.25f;

        uiText = uiTextObject.GetComponent<UnityEngine.UI.Text>();

        walkSpeed = devWalkingSpeed;
        //walkSpeed = realisticWalkingSpeed;

        //playerPosition = new Coordinate(55.9514069, -3.1881186); // edinburgh
        //playerPosition = new Coordinate(51.494027837496965, -0.12680686529219184); // london
        //playerPosition = new Coordinate(64.1314075, -21.9376544); // reykjavik
        //playerPosition = new Coordinate(35.6581864, 139.7009909); // shibuya
        //playerPosition = new Coordinate(-43.6263190, 172.7223080); // diamond harbour, NZ
        playerPosition = new Coordinate(40.70585663372225, -73.99655464966919); // ny

        int xTile;
        int yTile;

        // get the tile where the player is
        LatLongToTileNumbers(playerPosition.Latitude.DecimalDegree, playerPosition.Longitude.DecimalDegree, zoom, out xTile, out yTile);

        rootTile = await LoadTile(zoom, xTile, yTile);

        // place Player
        playerObject = (GameObject)Instantiate(playerPrefab);
        Vector3 playerWorldPosition = ECEFSubtract(playerPosition.ECEF, rootTileECEF);

        Vector3 playerOrientation = ECEFToVectorNoScale(playerPosition).normalized;

        playerWorldPosition += playerOrientation;

        playerObject.transform.position = playerWorldPosition;
        playerObject.transform.rotation = Quaternion.FromToRotation(Vector3.up, playerOrientation);
    }

    public async Task<GameObject> LoadTile(int zoom, int xTile, int yTile)
    {
        GameObject tileMesh = new GameObject();
        tileMesh.name = xTile + ", " + yTile + " @ " + zoom;
        MeshRenderer meshRenderer = tileMesh.AddComponent<MeshRenderer>();

        meshRenderer.material = new Material(Shader.Find("Standard"));

        string url = "https://a.tile.openstreetmap.org/" + zoom + "/" + xTile + "/" + yTile + ".png";

        meshRenderer.material.mainTexture = await GetRemoteTexture(url);

        //print(url);

        MeshFilter meshFilter = tileMesh.AddComponent<MeshFilter>();
        Mesh mesh = new Mesh();

        double tileBoundingBoxNlat = 0;
        double tileBoundingBoxWlng = 0;
        double tileBoundingBoxSlat = 0;
        double tileBoundingBoxElng = 0;

        TileNumbersToBoundingBox(xTile, yTile, zoom, out tileBoundingBoxNlat, out tileBoundingBoxSlat, out tileBoundingBoxElng, out tileBoundingBoxWlng);

        //print(tileBoundingBoxNlat);
        //print(tileBoundingBoxWlng);
        //print(tileBoundingBoxSlat);
        //print(tileBoundingBoxElng);

        List<Coordinate> coords = new List<Coordinate>();
        coords.Add(new Coordinate(tileBoundingBoxNlat, tileBoundingBoxWlng));
        coords.Add(new Coordinate(tileBoundingBoxNlat, tileBoundingBoxElng));
        coords.Add(new Coordinate(tileBoundingBoxSlat, tileBoundingBoxWlng));
        coords.Add(new Coordinate(tileBoundingBoxSlat, tileBoundingBoxElng));

        List<Vector3> vertices = new List<Vector3>();
        List<Vector3> normals = new List<Vector3>();

        ECEF tileOriginECEF = coords[0].ECEF;

        foreach (Coordinate c in coords)
        {
            Vector3 vertexNormal = ECEFToVector(c).normalized;

            Vector3 vertexPosition = ECEFSubtract(c.ECEF, coords[0].ECEF);

            vertices.Add(vertexPosition);

            //Debug.DrawLine(vertexPosition, vertexPosition + vertexNormal, Color.red, Mathf.Infinity);

            normals.Add(vertexNormal);
        }
        mesh.vertices = vertices.ToArray();

        int[] tris = new int[6]
        {
                            // lower left triangle
                            0, 1, 2,
                            // upper right triangle
                            2, 1, 3
        };
        mesh.triangles = tris;

        mesh.normals = normals.ToArray();

        Vector2[] uv = new Vector2[4]
        {
            new Vector2(0, 1),
            new Vector2(1, 1),
            new Vector2(0, 0),
            new Vector2(1, 0)
        };
        mesh.uv = uv;

        meshFilter.mesh = mesh;

        if (rootTileECEF == null)
        {
            rootTileECEF = tileOriginECEF;
        }
        else
        {
            Vector3 offset = ECEFSubtract(tileOriginECEF, rootTileECEF);

            tileMesh.transform.position = offset;
        }

        if (!tiles.ContainsKey(new KeyValuePair<int, int>(xTile, yTile)))
        {
            tiles.Add(new KeyValuePair<int, int>(xTile, yTile), tileMesh);
        }

        return tileMesh;
    }

    public static async Task<Texture2D> GetRemoteTexture(string url)
    {
        using (UnityWebRequest www = UnityWebRequestTexture.GetTexture(url))
        {
            // begin request:
            var asyncOp = www.SendWebRequest();

            // await until it's done: 
            while (asyncOp.isDone == false)
                await Task.Delay(1000 / 30);//30 hertz

            // read results:
            if (www.isNetworkError || www.isHttpError)
            {
                // log error:
#if DEBUG
            Debug.Log( $"{www.error}, URL:{www.url}" );
#endif

                // nothing to return on error:
                return null;
            }
            else
            {
                // return valid results:
                return DownloadHandlerTexture.GetContent(www);
            }
        }
    }

    public static void LatLongToTileNumbers(double lat, double lng, int zoom, out int xTileNumber, out int yTileNumber)
    {
        xTileNumber = (int)Math.Floor((lng + 180) / 360 * Math.Pow(2, zoom));
        yTileNumber = (int)Math.Floor((1 - Math.Log(Math.Tan(lat * Math.PI / 180) + 1 / Math.Cos(lat * Math.PI / 180)) / Math.PI) / 2 * Math.Pow(2, zoom));
    }

    private static void TileNumbersToBoundingBox(int xTileNumber, int yTileNumber, int zoom,
        out double tileBoundingBoxNlat, out double tileBoundingBoxSlat, out double tileBoundingBoxElng, out double tileBoundingBoxWlng)
    {
        tileBoundingBoxNlat = tile2lat((double)yTileNumber, zoom);
        tileBoundingBoxSlat = tile2lat((double)yTileNumber + 1, zoom);
        tileBoundingBoxWlng = tile2lon((double)xTileNumber, zoom);
        tileBoundingBoxElng = tile2lon((double)xTileNumber + 1, zoom);
    }

    static double tile2lon(double x, double z)
    {
        return x / Math.Pow(2.0, z) * 360.0 - 180;
    }

    static double tile2lat(double y, double z)
    {
        double n = Math.PI - (2.0 * Math.PI * y) / Math.Pow(2.0, z);
        return (Math.Atan(Math.Sinh(n))) * (180 / Math.PI);
    }

    // Unity == ECEF
    // X == Y * -1
    // Y == Z
    // Z == X

    // ECEF == Unity
    // X == Z
    // Y == X * -1
    // Z == Y
    async void Update()
    {
        string text = "";

        if (rootTileECEF != null && playerObject != null)
        {
            float translation = Input.GetAxis("Vertical") * walkSpeed;
            translation *= Time.deltaTime;

            float headingDelta = rotationSpeed * Input.GetAxis("Mouse X");
            heading += headingDelta;
            heading %= 360;

            // Get the ECEF location of the root tile in ECEF space
            double X = rootTileECEF.X;
            double Y = rootTileECEF.Y;
            double Z = rootTileECEF.Z;

            // Add the player position relative to the root tile, divide by scale because unity units ate in m and ECEF units are in km
            Vector3 playerPosition = playerObject.transform.position + (playerObject.transform.forward * translation);
            X += (playerPosition.z / scale);
            Y += ((playerPosition.x / scale) * -1);
            Z += (playerPosition.y / scale);

            // Get the coordinated of the player location on earth
            Coordinate playerCoordinates = ECEF.ECEFToLatLong(X, Y, Z);
            playerCoordinates.FormatOptions = new CoordinateFormatOptions() { Format = CoordinateFormatType.Decimal, Round = 15 };

            // Build a new Coordinate object using the lat/long from the previous step
            // this will assume the coords are on the WGS84 spheroid
            Coordinate playerCoordinatesSetHeight = new Coordinate(playerCoordinates.Latitude.DecimalDegree, playerCoordinates.Longitude.DecimalDegree);
            playerCoordinatesSetHeight.FormatOptions = new CoordinateFormatOptions() { Format = CoordinateFormatType.Decimal, Round = 15 };

            // Get the ECEF coordinates on the surface of the WGS84 ellipsoid
            ECEF playerECEF_0m = playerCoordinatesSetHeight.ECEF;
            Vector3 relativeLocation_0m = ECEFSubtract(playerECEF_0m, rootTileECEF);

            // Get the ECEF coordinates 1 m up from the WGS84 ellipsoid
            ECEF playerECEF_1m = new ECEF(playerCoordinatesSetHeight, new Distance(0.001));
            Vector3 relativeLocation_1m = ECEFSubtract(playerECEF_1m, rootTileECEF);

            // Get the ECEF coordinates 2 m up from the WGS84 ellipsoid
            ECEF playerECEF_2m = new ECEF(playerCoordinatesSetHeight, new Distance(0.002));
            Vector3 relativeLocation_2m = ECEFSubtract(playerECEF_2m, rootTileECEF);

            // Work out the direction of gravity, which way "down" is at this location on the WGS84 ellipsoid
            Vector3 gravityVector = (relativeLocation_0m - relativeLocation_1m).normalized;
            Quaternion gravityDirection = Quaternion.FromToRotation(Vector3.up, gravityVector);

            // put the gravity indicator 1m off the ground, pointing "down"
            arrowObject.transform.position = relativeLocation_1m;
            arrowObject.transform.rotation = gravityDirection;

            // player should be oriented away from gravity
            Quaternion playerOrientation = Quaternion.FromToRotation(Vector3.up, gravityVector * -1);

            // the player's origin is in the centre of the 2m tall capsule, so use the location 1m from the surface as the position
            // this kind of position update breaks physics, so should probably work out some way of shoving the player to this new position
            // by subtracting the current position from the new, then using addforce?
            Vector3 delta = relativeLocation_1m - playerObject.transform.position;
            playerObject.transform.position = relativeLocation_1m;

            // set the player to point away from gravity
            // this kind of rotation update breaks physics, so should probably work out some way of shoving the player to this new rotation
            // by subtracting the current rotation from the new, then using addforce?
            playerObject.transform.rotation = playerOrientation;

            // add the left-right rotation to the player's rotation
            playerObject.transform.Rotate(0, heading, 0);

            // set the indicators to the surface and 2m
            lowerPoint.transform.position = relativeLocation_0m;
            upperPoint.transform.position = relativeLocation_2m;

            text += playerCoordinates + " @ " + playerCoordinates.ECEF.GeoDetic_Height.Meters + " -> " + playerCoordinates.ECEF + Environment.NewLine;
            text += playerCoordinatesSetHeight + " @ " + playerECEF_1m.GeoDetic_Height.Meters + " -> " + playerECEF_1m + Environment.NewLine;

            LoadTilesForPlayerLocation(playerCoordinatesSetHeight.Latitude.DecimalDegree, playerCoordinatesSetHeight.Longitude.DecimalDegree);

            if (Input.GetKeyDown("space"))
            {
                print("space key was pressed");
                string f = "" + playerCoordinatesSetHeight;
                url += f.Replace(" ", ",") + "/";
                print(url);
            }
        }

        uiText.text = text;
    }

    public void LoadTilesForPlayerLocation(double lat, double lng)
    {
        int tileX;
        int tileY;

        // get the tile where the player is
        LatLongToTileNumbers(lat, lng, zoom, out tileX, out tileY);

        // if the player has moved tile
        if (currentTileX != tileX || currentTileY != tileY)
        {
            currentTileX = tileX;
            currentTileY = tileY;

            //print("Moved to tile " + currentTileX + "," + currentTileY);
            int tileDistance = 2;
            for (int x = currentTileX - tileDistance; x < currentTileX + tileDistance; x++)
            {
                for (int y = currentTileY - tileDistance; y < currentTileY + tileDistance; y++)
                {
                    if (!tiles.ContainsKey(new KeyValuePair<int, int>(x, y)))
                    {
                        //print("Loading tile " + x + "," + y);
                        LoadTile(zoom, x, y);
                    }
                }
            }
        }
    }

    public Vector3 ECEFToVectorNoScale(Coordinate toConvert)
    {
        double X = toConvert.ECEF.X;
        double Y = (toConvert.ECEF.Y) * -1;
        double Z = toConvert.ECEF.Z;

        return new Vector3((float)Y, (float)Z, (float)X);
    }

    public Vector3 ECEFToVector(Coordinate toConvert)
    {
        double X = toConvert.ECEF.X * scale;
        double Y = (toConvert.ECEF.Y * scale) * -1;
        double Z = toConvert.ECEF.Z * scale;

        return new Vector3((float)Y, (float)Z, (float)X);
    }

    public Vector3 ECEFSubtract(ECEF a, ECEF b)
    {
        double X = (a.X * scale) - (b.X * scale);
        double Y = ((a.Y * scale) - (b.Y * scale)) * -1;
        double Z = (a.Z * scale) - (b.Z * scale);

        // Unity == ECEF
        // X == Y * -1
        // Y == Z
        // Z == X

        // ECEF == Unity
        // X == Z
        // Y == X * -1
        // Z == Y

        return new Vector3((float)Y, (float)Z, (float)X);
    }
}