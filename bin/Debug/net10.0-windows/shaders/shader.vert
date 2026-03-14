#version 330 core

layout(location=0) in vec3 aPosition;
layout(location=1) in vec2 aTexCoord;
layout(location=2) in vec4 aColorNormal;

out vec2 fTexCoord;
out vec4 fColorNormal;

uniform mat4 model;
uniform mat4 view;
uniform mat4 projection;

uniform int isBillboard;
uniform int isWaterBox;
uniform int timer;
uniform int useLight;

uniform float scale;

void main()
{
	mat4 mv = view * model;

	if(isBillboard == 1)
	{
		mv[0][0] = 1/scale;
		mv[0][1] = 0;
		mv[0][2] = 0;
		mv[0][3] = 0;

		mv[1][0] = 0;
		mv[1][1] = 1/scale;
		mv[1][2] = 0;
		mv[1][3] = 0;

		mv[2][0] = 0;
		mv[2][1] = 0;
		mv[2][2] = 1/scale;
		mv[2][3] = 0;
	}

	gl_Position = projection * mv * vec4(aPosition, 1.0);
	
	if(isWaterBox == 0)
		fTexCoord = aTexCoord;
	else
	{
		// water box
		fTexCoord = aTexCoord + timer / 1000.0;
	}
	
	if(useLight == 0)
		fColorNormal = aColorNormal;
	else
	{
		fColorNormal = transpose(inverse(model)) * vec4(aColorNormal.xyz, 1.0);
	}
}