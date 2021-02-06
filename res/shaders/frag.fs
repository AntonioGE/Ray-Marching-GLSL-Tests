#version 410 core

//Window
uniform vec2 winScale;
uniform vec2 winOffset;

//Camera 
uniform vec3 camPos;
uniform mat3 camTransf;
uniform float tan;

//Constants
int MAX_STEPS_RAY = 100;
float MAX_DIST = 100.0;
float SURF_DIST = 0.01;

float hue[] = {0.5, 0.638};
float glowHue = 0.5;

float sphereDist(vec3 point, vec3 sphereCoords, float sphereRadius){
    return length(point - sphereCoords) - sphereRadius;
}

float manySpheres(vec3 point){
    point.xy = mod((point.xy), 1.0) - vec2(0.5);
    return length(point)-0.3;
    //point = mod(point, 1.0) - vec3(0.5);
    //return length(point)-0.3;
}

float getDist(vec3 point){
    //return min(sphereDist(point, vec3(0, 0, 0), 1.0), sphereDist(point, vec3(2, 0, 0), 1.0));
    //return sphereDist(point, vec3(0, 0, 0), 0.1);
    return manySpheres(point);
}

vec2 rayMarch(vec3 from, vec3 dir, inout int objIndex){
    float totalDist = 0.0; 
    int i;
    for(i = 0; i < MAX_STEPS_RAY; i++){
        float dist = getDist(from + dir * totalDist);
        totalDist += dist;
        if(totalDist > MAX_DIST || dist < SURF_DIST){
            objIndex = 0;
            break;
        }
    }
    objIndex = 1;
    return vec2(totalDist, i);
}

vec3 rgb2hsv(vec3 c)
{
    vec4 K = vec4(0.0, -1.0 / 3.0, 2.0 / 3.0, -1.0);
    vec4 p = mix(vec4(c.bg, K.wz), vec4(c.gb, K.xy), step(c.b, c.g));
    vec4 q = mix(vec4(p.xyw, c.r), vec4(c.r, p.yzx), step(p.x, c.r));

    float d = q.x - min(q.w, q.y);
    float e = 1.0e-10;
    return vec3(abs(q.z + (q.w - q.y) / (6.0 * d + e)), d / (q.x + e), q.x);
}

vec3 hsv2rgb(vec3 c)
{
    vec4 K = vec4(1.0, 2.0 / 3.0, 1.0 / 3.0, 3.0);
    vec3 p = abs(fract(c.xxx + K.xyz) * 6.0 - K.www);
    return c.z * mix(K.xxx, clamp(p - K.xxx, 0.0, 1.0), c.y);
}

void main(){
    vec2 uv = gl_FragCoord.xy * winScale + winOffset;
    
    vec3 from = camPos;
    vec3 dir = camTransf * normalize(vec3(uv, -1.0));

    int objIndex;
    vec2 dist_steps = rayMarch(from, dir, objIndex);
    float dist = dist_steps.x / MAX_DIST;
    float steps = dist_steps.y / MAX_STEPS_RAY;
    
    float sat = 1.0;
    vec3 hsvDist = vec3(hue[objIndex], sat, dist);
    vec3 hsvGlow = vec3(glowHue, sat, steps);
    vec3 rgb = hsv2rgb(vec3(mix(hsvDist.x, hsvGlow.x, steps), sat, max(1.0 - dist, steps)));
    gl_FragColor = vec4(rgb, 1.0);

    //vec3 rgb = hsv2rgb(vec3(mix(hue[objIndex], glowHue, steps), 0.5, steps));
    //gl_FragColor = vec4(rgb, 1.0);

    //vec3 rgb = hsv2rgb(vec3(hue[objIndex], 0.5, steps));
    //gl_FragColor = vec4(value, value, value, 1.0);
}
