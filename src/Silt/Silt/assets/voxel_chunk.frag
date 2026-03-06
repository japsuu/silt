in vec3 f_color;
in vec3 f_normal;

out vec4 FragColor;

void main()
{
    vec3 color = f_color * 0.5 + f_normal * 0.5;
    FragColor = vec4(color, 1.0);
}

