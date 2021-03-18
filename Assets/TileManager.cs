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
    float devWalkingSpeed = 10f;

    float heading = 0;

    double scale = 1000;

    // Start is called before the first frame update
    async void Start()
    {
        uiText = uiTextObject.GetComponent<UnityEngine.UI.Text>();

        walkSpeed = devWalkingSpeed;

        //playerPosition = new Coordinate(55.9514069, -3.1881186); // edinburgh
        playerPosition = new Coordinate(51.494027837496965, -0.12680686529219184); // london
        //playerPosition = new Coordinate(64.1314075, -21.9376544); // reykjavik
        //playerPosition = new Coordinate(35.6581864, 139.7009909); // shibuya
        //playerPosition = new Coordinate(-43.6263190, 172.7223080); // diamond harbour, NZ

        int xTile;
        int yTile;

        // get the tile where the player is
        LatLongToTileNumbers(playerPosition.Latitude.DecimalDegree, playerPosition.Longitude.DecimalDegree, zoom, out xTile, out yTile);

        rootTile = await LoadTile(zoom, xTile, yTile);

        print(xTile);
        print(yTile);

        // place Player
        playerObject = (GameObject)Instantiate(playerPrefab);
        Vector3 playerOrientation = ECEFToVector(playerPosition).normalized;

        Vector3 playerWorldPosition = ECEFToVector(playerPosition);
        //playerWorldPosition += playerOrientation;

        //playerObject.transform.position = playerWorldPosition;
        playerObject.transform.rotation = Quaternion.FromToRotation(Vector3.up, playerOrientation);
    }

    public async Task<GameObject> LoadTile(int zoom, int xTile, int yTile)
    {
        GameObject tileMesh = new GameObject();
        tileMesh.name = xTile + ", " + yTile + " @ " + zoom;
        MeshRenderer meshRenderer = tileMesh.AddComponent<MeshRenderer>();

        meshRenderer.material = new Material(Shader.Find("Standard"));

        meshRenderer.material.mainTexture = await GetRemoteTexture("https://a.tile.openstreetmap.org/" + zoom + "/" + xTile + "/" + yTile + ".png");

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

    // Update is called once per frame
    async void Update()
    {
        string text = "";

        if (rootTileECEF != null)
        {
            text += "Origin: " + rootTileECEF + Environment.NewLine;
        }

        if (playerObject != null)
        {
            text += "Player: " + playerObject.transform.position + Environment.NewLine;
        }

        if (rootTileECEF != null && playerObject != null)
        {
            double X = rootTileECEF.X;
            double Y = rootTileECEF.Y;
            double Z = rootTileECEF.Z;

            X += (playerObject.transform.position.z / scale);
            Y += (playerObject.transform.position.x / scale) * -1;
            Z += (playerObject.transform.position.y / scale);

            text += "World position ECEF: " + X + ", " + Y + ", " + Z + Environment.NewLine;

            Coordinate c = ECEF.ECEFToLatLong(X, Y, Z);
            c.FormatOptions = new CoordinateFormatOptions() { Format = CoordinateFormatType.Decimal, Round = 15 };

            text += "World position GPS: " + c + Environment.NewLine;

            if (Input.GetKeyDown("space"))
            {
                print("space key was pressed");
                string f = "" + c;
                url += f.Replace(" ", ",") + "/";
                print(url);
            }

            int tileX;
            int tileY;

            // get the tile where the player is
            LatLongToTileNumbers(c.Latitude.DecimalDegree, c.Longitude.DecimalDegree, zoom, out tileX, out tileY);

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

            text += "Tile: " + currentTileX + ", " + currentTileY + Environment.NewLine;

            Vector3 playerOrientation = ECEFToVector(c).normalized;

            text += "o: " + playerOrientation.ToString("F8");

            float h = rotationSpeed * Input.GetAxis("Mouse X");
            heading += h;
            heading %= 360;

            playerObject.transform.rotation = Quaternion.FromToRotation(Vector3.up, playerOrientation);
            playerObject.transform.Rotate(0, heading, 0, Space.Self);

            float translation = Input.GetAxis("Vertical") * walkSpeed;
            translation *= Time.deltaTime;
            playerObject.transform.Translate(Vector3.forward * translation);
        }

        uiText.text = text;
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

        return new Vector3((float)Y, (float)Z, (float)X);
    }
}