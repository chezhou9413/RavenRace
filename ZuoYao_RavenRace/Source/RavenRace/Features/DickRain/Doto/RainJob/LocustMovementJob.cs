using RavenRace.Features.DickRain.Doto.Data;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

[BurstCompile(CompileSynchronously = true)]
public struct LocustMovementJob : IJobParallelFor
{
    public NativeArray<LocustData> locusts;
    public float deltaTime;
    public float time;
    public float2 windDir;
    public float mapW;
    public float mapH;
    public int activeCount;

    public void Execute(int index)
    {
        if (index >= activeCount) return;

        LocustData locust = locusts[index];

        float wave1 = math.sin(time * 3.0f + locust.randomSeed);
        float wave2 = math.cos(time * 7.0f + locust.randomSeed * 0.5f);
        float noise = wave1 + wave2 * 0.5f;
        float2 sideVec = new float2(-windDir.y, windDir.x);
        float2 flyDir = math.normalize(windDir + sideVec * noise * 0.7f);

        locust.position += flyDir * locust.speed * deltaTime;
        locust.angle = math.degrees(math.atan2(flyDir.x, flyDir.y));

        if (locust.position.x > mapW + 5f)
            locust.position.x -= mapW + 10f;
        else if (locust.position.x < -5f)
            locust.position.x += mapW + 10f;

        if (locust.position.y > mapH + 5f)
            locust.position.y -= mapH + 10f;
        else if (locust.position.y < -5f)
            locust.position.y += mapH + 10f;

        locusts[index] = locust;
    }
}