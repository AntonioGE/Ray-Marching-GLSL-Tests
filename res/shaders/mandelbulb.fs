#version 410 core

//Window
uniform vec2 winScale;
uniform vec2 winOffset;

//Camera 
uniform vec3 camPos;
uniform mat3 camTransf;
uniform float tan;

//Sun 
uniform vec3 lightPos;

//Hue
uniform float hueOffset;

//Sphere
uniform float radius;

//Constants
int MAX_STEPS_RAY = 100;
float MAX_DIST = 100.0;
float SURF_DIST = 0.001;

int iterations = 20;
float bailout = 2.0;
uniform float power = 8;

const int VOID_ID = 0;
const int PLANE_ID = 1;
const int SPHERE_ID = 2;

float smoothMin(float dstA, float dstB, float k){
    float h = max(k - abs(dstA-dstB), 0) / k;
    return min(dstA, dstB) - h*h*h*k*1/6.0;
}

float planeDist(vec3 point){
    return abs(point.z + 2);
}

float sphereDist(vec3 point, vec3 sphereCoords, float sphereRadius){
    return length(point - sphereCoords) - sphereRadius;
}

float getDist(vec3 point, inout int objIndex){
    float planeDist = planeDist(point);
    float sphereDist = sphereDist(point, vec3(0.0, 0.0, 0.0), radius);
    if(planeDist < sphereDist){
        objIndex = PLANE_ID;
        return planeDist;
    }else{
        objIndex = SPHERE_ID;
        return sphereDist;
    }
}

/*
float getDist(vec3 point){
    return min(planeDist(point), sphereDist(point, vec3(0.0, 0.0, 0.0), 1.0));
}*/


float mandle(vec3 pos) {
    vec3 z = pos;
    float dr = 1.0;
    float r = 0.0;
    for (int i = 0; i < iterations ; i++) {
        r = length(z);
        if (r>bailout) break;

        float theta = acos(z.z/r);
        float phi = atan(z.y,z.x);
        dr =  pow( r, power-1.0)*power*dr + 1.0;

        // scale and rotate the point
        float zr = pow( r,power);
        theta = theta*power;
        phi = phi*power;

        // convert back to cartesian coordinates
        z = zr*vec3(sin(theta)*cos(phi), sin(phi)*sin(theta), cos(theta));
        z+=pos;
    }
    return 0.5*log(r)*r/dr;
}

float getDist(vec3 point){
    //return min(planeDist(point), mandle(point));
    return mandle(point);
    //return min(mandle(point), sphereDist(point, vec3(0.0, 0.0, 0.0), 1.0));
    //return smoothMin(mandle(point), sphereDist(point, vec3(0.0, 0.0, 0.0), radius), 0.1);
    //return max(mandle(point), -sphereDist(point, vec3(0.0, 0.0, 1.0), 0.5));
}

vec3 getNormal(vec3 point) {
    float d = getDist(point);
    vec2 e = vec2(0.00001, 0);

    vec3 n = d - vec3(
        getDist(point - e.xyy),
        getDist(point - e.yxy),
        getDist(point - e.yyx));

    return normalize(n);
}

float shadow(vec3 from, vec3 dir, float mint, float maxt ) {
    for(float t = mint; t < maxt; ) {
        float h = getDist(from + dir * t);
        if(h < 0.001){
            return 0.0;
        }
        t += h;
    }
    return 1.0;
}

float softShadow(vec3 from, vec3 dir, float mint, float maxt, float k ){
    float res = 1.0;
    for(float t = mint; t < maxt;) {
        float h = getDist(from + dir * t);
        if( h < 0.001 )
            return 0.0;
        res = min( res, k*h/t );
        t += h;
    }
    return res;
}

float softerShadow( vec3 from, vec3 dir, float mint, float maxt, float k )
{
    float res = 1.0;
    float ph = 1e20;
    for( float t=mint; t<maxt; )
    {
        float h = getDist(from + dir * t);
        if( h<0.001 )
            return 0.0;
        float y = h*h/(2.0*ph);
        float d = sqrt(h*h-y*y);
        res = min( res, k*d/max(0.0,t-y) );
        ph = h;
        t += h;
    }
    return res;
}

vec2 rayMarch(vec3 from, vec3 dir, inout int objIndex, inout float diffuse, inout float specular){
    float totalDist = 0.0; 
    int i;
    for(i = 0; i < MAX_STEPS_RAY; i++){
        vec3 point = from + dir * totalDist;
        //float dist = getDist(point, objIndex);
        float dist = getDist(point);
        objIndex = SPHERE_ID;
        totalDist += dist;
        if(dist < SURF_DIST){
            vec3 lightDir = normalize(lightPos - point);
            vec3 normal = getNormal(point);
            diffuse = max(0.2, dot(normal, lightDir));

            vec3 surf = from + dir * totalDist;
            vec3 surfLight = normalize(lightPos - surf);
            //float shad = max(0.2, shadow(surf, surfLight, 0.01, 10.0));
            float shad = max(0.2, softShadow(surf, surfLight, 0.01, 10.0, 8.0));
            diffuse = diffuse * shad;
            
            specular = pow(diffuse, 8);
            
            //vec3 refl = dir - 2.0 * dot(dir, normal) * normal;
            //specular = (dot(refl, lightDir) + 0.1) * shad;

            return vec2(totalDist, i);
        }else if(totalDist > MAX_DIST){
            objIndex = VOID_ID;
            return vec2(totalDist, i);
        }
    }
    objIndex = VOID_ID;
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

    int objIndex = VOID_ID;
    float diffuse = 0.0;
    float specular = 0.0;
    vec2 dist_steps = rayMarch(from, dir, objIndex, diffuse, specular);
    float dist = dist_steps.x / MAX_DIST;
    float steps = dist_steps.y / MAX_STEPS_RAY;

    float sat = 1.0;
    //vec3 hsv = vec3(hue[objIndex], sat, diffuse);
    vec3 hsv = vec3(steps / 2.0 + hueOffset, sat - specular, max(diffuse * (1 - dist) , steps));
    gl_FragColor = vec4(hsv2rgb(hsv), 1.0);
    //vec3 hsvGlow = vec3(glowHue, sat, steps);
    //vec3 rgb = hsv2rgb(vec3(mix(hsvDist.x, hsvGlow.x, steps), sat, min(diffuse, max(1.0 - dist, steps))));
    //gl_FragColor = vec4(rgb, 1.0);
}