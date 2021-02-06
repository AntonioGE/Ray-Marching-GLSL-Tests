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

float sphereDist(vec3 point, vec3 sphereCoords, float sphereRadius){
    return length(point - sphereCoords) - sphereRadius;
}

float getDist(vec3 point){
    return min(sphereDist(point, vec3(0, 0, 0), 1), sphereDist(point, vec3(2, 0, 0), 1));
}

vec2 rayMarch(vec3 from, vec3 dir){
    float totalDist = 0.0; 
    int i;
    for(i = 0; i < MAX_STEPS_RAY; i++){
        float dist = getDist(from + dir * totalDist);
        totalDist += dist;
        if(totalDist > MAX_DIST || dist < SURF_DIST) break;
    }
    return vec2(totalDist, i);
}

void main(){
    vec2 uv = gl_FragCoord.xy * winScale + winOffset;
    
    vec3 from = camPos;
    vec3 dir = camTransf * normalize(vec3(uv, -1.0));

    vec2 dist_steps = rayMarch(from, dir);
    float value = dist_steps.y / MAX_STEPS_RAY;

    gl_FragColor = vec4(value, value, value, 1.0);
}
