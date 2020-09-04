struct WindFieldPoint
{
    //https://developer.nvidia.com/content/understanding-structured-buffer-performance
    float3 pos;
    float3 wind;
    uint depth;
    int priority;
    int mode;
};