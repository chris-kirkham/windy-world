//hashes a given position into a 3D field of cells. Does not perform any out-of-bounds checks,
//so negative indexes and indexes greater than the field size will be possible
int3 hash(float3 pos, float cellSize, float3 fieldStart)
{
	float3 relativePos = pos - fieldStart;
	return int3(floor(relativePos.x / cellSize), floor(relativePos.y / cellSize), floor(relativePos.z / cellSize));
}

/*
int fastFloor(float n)
{
	return (int)(f + 32768f) - 32768;
}
*/