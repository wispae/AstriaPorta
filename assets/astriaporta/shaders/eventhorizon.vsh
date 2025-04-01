#version 330 core
#extension GL_ARB_explicit_attrib_location: enable

layout(location = 0) in vec3 vertexPositionIn;
layout(location = 1) in vec3 vertexNormalIn;
layout(location = 2) in vec2 uvIn;
layout(location = 3) in vec4 colorIn;
layout(location = 4) in int flags;

uniform mat4 projectionMatrix;
uniform mat4 viewMatrix;
uniform mat4 modelMatrix;

uniform sampler2D tex0;

uniform int dontWarpVertices;
uniform int addRenderFlags;
uniform vec4 rgbaTint;
uniform float tIn;
uniform float noiseOffset;
uniform vec3 normalIn;

out vec4 color;
out vec2 uv;
out vec3 normal;
out vec4 camPos;
out vec4 worldPos;
out vec4 basePos;
out vec4 modPos;

#include vertexflagbits.ash
#include noise2d.ash
#include vertexwarp.vsh

void main()
{
	worldPos = modelMatrix * vec4(vertexPositionIn, 1.0);
    worldPos = applyVertexWarping(flags | addRenderFlags, worldPos);
    worldPos = applyGlobalWarping(worldPos);

	basePos = worldPos;

	vec4 offsetColor = texture(tex0, uvIn);
	float tMult = abs(sin(tIn));

	worldPos.z += normalIn.z * offsetColor.r * tMult * 4f * vertexNormalIn.x;
	worldPos.x += normalIn.x * offsetColor.r * tMult * 4f * vertexNormalIn.x;

	if (distance(offsetColor.g - 0.5, 0f) > .05f) {
		worldPos.x += normalIn.z * (offsetColor.g - 0.5f) * tMult * .75f * vertexNormalIn.x;
		worldPos.z += normalIn.x * (offsetColor.g - 0.5f) * tMult * .75f * vertexNormalIn.x;
	}
	if (distance(offsetColor.b - 0.5, 0f) > .05f) {
		worldPos.y += (offsetColor.b - 0.5f) * tMult * .75f * vertexNormalIn.x;
	}

	worldPos.z += vertexNormalIn.x * normalIn.z * cnoise2(vertexPositionIn.xy * noiseOffset) * 0.1f;
	worldPos.x += vertexNormalIn.x * normalIn.x * cnoise2(vertexPositionIn.zy * noiseOffset) * 0.1f;

	modPos = worldPos;

	camPos = viewMatrix * worldPos;
	uv = uvIn;
	
	color = rgbaTint;
	color.a *= clamp(20 * (1.10 - length(worldPos.xz) / 100) - 5, -1, 1);

	gl_Position = projectionMatrix * camPos;

	normal = normalize((modelMatrix * vec4(normalIn.x, normalIn.y, normalIn.z, 0)).xyz);
}