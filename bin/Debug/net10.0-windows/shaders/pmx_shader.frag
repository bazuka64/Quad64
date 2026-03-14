#version 330 core

out vec4 Color;

in vec2 fUV;
in vec3 fNormal;

uniform sampler2D texture0;

void main()
{
	vec4 texColor = texture(texture0, fUV);

	Color = texColor;

	//vec3 norm = normalize(fNormal);
	//vec3 lightDir = normalize(vec3(1,1,1));
	//float diff = max(dot(norm, lightDir), 0.0);
	//
	//vec3 ambient = vec3(0.1, 0.1, 0.1);
	//
	//Color = vec4(texColor.rgb * diff + ambient, texColor.a);
}