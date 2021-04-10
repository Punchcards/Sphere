# Sphere

This is an initial attempt at creating an explorable world in Unity that is the same size and shape as Earth.

Video demo:

[![](http://img.youtube.com/vi/MIlY8yZn2y8/0.jpg)](https://www.youtube.com/watch?v=MIlY8yZn2y8 "")

## Method

Because the real planet Earth is way too big to fit into Unity, I'm generating tiles around the player.

1. Start the user at a real GPS coordinate
2. Work out what Tile the user is stood on using a [Zoom Level](https://wiki.openstreetmap.org/wiki/Zoom_levels) and their location
3. Get the coordinates of the 4 corners of the Tile (NW, NE, SW, SE), and convert those coordinates to [ECEF](https://en.wikipedia.org/wiki/ECEF)
4. The ECEF values make the tiles way too far from Unity's center, so subtract an origin point from their position, currently the first Tile loaded
5. This means that the Tiles should be the same size/shape as the real world, and the player can walk around on them

## Features

1. You can visit anywhere on the map, but OSM only has tiles between the poles, not including them
2. You can press space at a few points while walking, then load the URL in a browser to see the points
3. You can walk around .. sortof

## Issues

1. The current method of moving is to work out the new position coords each step and move the player to that position, which breaks Unity Physics.
2. There's no world re-centering yet, so if you walk too far away from where you start, float precision will become an issue
3. You have to visit [Open Street Map](https://www.openstreetmap.org/) before they'll serve you image tiles.

## Fixed issues

1. The player capsule I'm using currently, sinks into the map if you head South, and floats up away from the earth if you head North. This I've not managed to work out why.
  Turns out I was normalising the ECEF position vector, which makes the orientation act as if the Earth is spherical, which it ain't. New method uses GPS coordinate library to clamp coords to 0m from the WGS84 oblate spheroid. 

Please feel free to create PRs or make suggestions, I've mainly put this here to get feedback on how to make it work better :)
