in vec2 f_uv;

out vec4 FragColor;

uniform sampler2D u_texture;

void main()
{
    vec4 col = texture(u_texture, f_uv);
    FragColor = col * vec4(f_uv, 0.0, 1.0);
}