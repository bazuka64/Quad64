#version 330 core

layout(location = 0) in vec3 aPosition;
layout(location = 1) in vec3 aNormal;
layout(location = 2) in vec2 aUV;
layout(location = 3) in vec4 aBones;
layout(location = 4) in vec4 aWeights;

out vec2 fUV;
out vec3 fNormal;

uniform mat4 model;
uniform mat4 view;
uniform mat4 projection;

uniform Matrices
{
	mat4 boneMatrix[400];
};

void main()
{
	vec4 pos = vec4(aPosition, 1.0);

	vec4 skinnedPos = vec4(0);
	skinnedPos     += (boneMatrix[int(aBones.x)] * pos) * (aWeights.x == 0 ? 1 : aWeights.x);
	skinnedPos	   += (boneMatrix[int(aBones.y)] * pos) * aWeights.y;
	skinnedPos	   += (boneMatrix[int(aBones.z)] * pos) * aWeights.z;
	skinnedPos	   += (boneMatrix[int(aBones.w)] * pos) * aWeights.w;

	gl_Position = projection * view * model * skinnedPos;
	fUV = aUV;
	fNormal = (transpose(inverse(model)) * vec4(aNormal, 1.0)).xyz;
}