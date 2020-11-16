//Converts 3D integer coordinates into 1D array indexes, given width (max x value + 1)
//and height (max y value + 1). Does NOT preserve spatial locality
int Index(int x, int y, int z, int width, int height)
{
    return x + (width * y) + (width * height * z);
}

int Index(int3 xyz, int width, int height)
{
    return Index(xyz.x, xyz.x, xyz.z, width, height);
}

//https://stackoverflow.com/questions/11316490/convert-a-1d-array-index-to-a-3d-array-index
int3 UnIndex(uint i, uint width, uint height)
{
    uint x = i % width;
    uint y = (i / width) % height;
    uint z = i / (width * height);

    return int3(x, y, z);
}