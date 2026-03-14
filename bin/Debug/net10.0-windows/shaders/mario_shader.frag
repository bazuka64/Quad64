#version 330 core

out vec4 Color;

in vec2 fTexCoord;
in vec3 fColor;
in vec3 fNormal;

uniform sampler2D texture0;

void main()
{
	vec4 texColor = texture(texture0, fTexCoord);

	Color.rgb = mix(fColor, texColor.rgb, texColor.a);
	Color.a = max(1, texColor.a);

	vec3 diffuseLightNormal = normalize(vec3(-.5, -1, -.5));
    float diffuseLightAmount = max(-dot(fNormal, diffuseLightNormal), 0);
    float ambientLightAmount = .3;
    float lightAmount = min(ambientLightAmount + diffuseLightAmount, 1);
    Color.rgb = mix(Color.rgb, Color.rgb * lightAmount, 1);
}