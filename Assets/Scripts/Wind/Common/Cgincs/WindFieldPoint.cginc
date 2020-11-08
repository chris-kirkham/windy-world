struct WindFieldPoint
{
    //https://developer.nvidia.com/content/understanding-structured-buffer-performance
    float3 pos;
    float3 wind;
    int priority;
    int mode;
};