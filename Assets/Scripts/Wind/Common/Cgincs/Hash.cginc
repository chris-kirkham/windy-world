//hashes a given position into a 3D field of cells. Does not perform any out-of-bounds checks,
//so indexes which would be negative (but wrap around due to returning a uint) and indexes greater than the field size will be possible
uint3 hash(float3 pos, float cellSize, float3 fieldStart)
{
	float3 relativePos = pos - fieldStart;
	return uint3(floor(relativePos.x / cellSize), floor(relativePos.y / cellSize), floor(relativePos.z / cellSize));
}

/*
int fastFloor(float n)
{
	return (int)(f + 32768f) - 32768;
}
*/