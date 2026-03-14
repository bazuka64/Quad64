#version 330 core

in vec2 fTexCoord;
in vec4 fColorNormal;

out vec4 FragColor;

uniform sampler2D texture0;
uniform int useTexture;
uniform int useLight;

uniform vec3 diffuseColor;
uniform vec3 diffuseDirection;
uniform vec3 ambientColor;

void main()
{
	vec4 texColor;
	if(useTexture == 0)
	{
		// not use Texture
		texColor = vec4(1,1,1,1);
	}
	else
	{
		// use Texture
		texColor = texture(texture0, fTexCoord);
	}

	vec4 Color;
	if(useLight == 0)
	{
		// not use light
		Color = texColor * fColorNormal;
	}
	else
	{
		// use light
		vec3 norm = normalize(fColorNormal.xyz);
		vec3 lightDir = normalize(diffuseDirection);

		float diff = max(dot(norm, lightDir), 0.0);

		Color = texColor * vec4(diffuseColor * diff + ambientColor, 1);
	}

	if(Color.a < 0.1)
		discard;
	FragColor = Color;
}