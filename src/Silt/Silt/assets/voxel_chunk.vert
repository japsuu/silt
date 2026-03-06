layout (location = 0) in vec3 v_pos;
layout (location = 1) in vec3 v_color;
layout (location = 2) in vec3 v_normal;

uniform mat4 u_mat_view;
uniform mat4 u_mat_proj;

out vec3 f_color;
out vec3 f_normal;

void main()
{
    gl_Position = u_mat_proj * u_mat_view * vec4(v_pos, 1.0);
    f_color = v_color;
    f_normal = v_normal;
}

