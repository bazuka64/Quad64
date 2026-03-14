#version 330 core

layout(location = 0) in vec3 aPosition;
layout(location = 1) in vec2 aTexCoord;
layout(location = 2) in vec3 aColor;
layout(location = 3) in vec3 aNormal;

out vec2 fTexCoord;
out vec3 fColor;
out vec3 fNormal;

uniform mat4 model;
uniform mat4 view;
uniform mat4 projection;

void main()
{
	gl_Position = projection * view * model * vec4(aPosition, 1.0);
	fTexCoord = aTexCoord;
	fColor = aColor;
	fNormal = normalize(model * vec4(aNormal, 0)).xyz;
}