#version 330 core
#extension GL_ARB_explicit_attrib_location: enable

layout(location = 0) out vec4 outColor;
layout(location = 1) out vec4 outGlow;

uniform float tIn;

in vec4 color;
in vec2 uv;
in vec3 normal;
in vec4 camPos;
in vec4 worldPos;
in vec4 basePos;
in vec4 modPos;

void main()
{
	float dis = distance(modPos, basePos);

	if (dis <= 0.1) {
		outColor = mix(color, vec4(1.0, 1.0, 1.0, 1.0), min(dis * 6.0, 1.0));
	} else {
		outColor = mix(color, vec4(1.0, 1.0, 1.0, 0.9), min(dis/3.0, 1.0));
	}
	outGlow = vec4(0.5,0.5,0.5,0.0);
}