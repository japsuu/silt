layout (location = 0) in vec3 v_pos;
layout (location = 1) in vec2 v_uv;

uniform mat4 u_mat_m;
uniform mat4 u_mat_mv;
uniform mat4 u_mat_mvp;

out vec2 f_uv;

void main()
{
    vec3 viewPos = (u_mat_mv * vec4(v_pos, 1.0)).xyz;
    vec3 worldPos = (u_mat_m * vec4(v_pos, 1.0)).xyz;
    
    gl_Position = u_mat_mvp * vec4(v_pos, 1.0);
    f_uv = v_uv;
}