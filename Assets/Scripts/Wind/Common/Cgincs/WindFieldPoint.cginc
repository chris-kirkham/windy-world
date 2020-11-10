struct WindFieldPoint
{
    //https://developer.nvidia.com/content/understanding-structured-buffer-performance
    float3 pos;
    float3 wind;
    int priority;
    int mode;
};

WindFieldPoint ConstructWindFieldPoint(float3 pos, float3 wind, int priority, int mode)
{
    WindFieldPoint windFieldPoint;
    windFieldPoint.pos = pos;
    windFieldPoint.wind = wind;
    windFieldPoint.priority = priority;
    windFieldPoint.mode = mode;

    return windFieldPoint;
}