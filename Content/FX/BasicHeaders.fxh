
struct TileData
{
    
};

uint getLineId(uint3 coordinates, uint3 size)
{
    return coordinates.x + 
        coordinates.y * size.x +
        coordinates.z * size.x * size.y;
}
uint getLineId(uint2 coordinates, uint3 size)
{
    return coordinates.x +
        coordinates.y * size.x;
}
