#version 330 core
#extension GL_ARB_explicit_attrib_location: enable

layout(location = 0) out vec4 outColor;
layout(location = 1) out vec4 outGlow;

uniform float tIn;

// uniform sampler2D tex0;

in vec4 color;
in vec2 uv;
in vec3 normal;
in vec4 camPos;
in vec4 worldPos;
in vec4 basePos;
in vec4 modPos;

void main()
{
	
	// outColor = color;
	// outColor = texture(tex0, uv);
	//outGlow = color;
	float dis = distance(modPos, basePos);

	if (dis <= 0.1f) {
		outColor = mix(color, vec4(1.0f, 1.0f, 1.0f, 1.0f), min(dis * 6.0f, 1.0f));
	} else {
		outColor = mix(color, vec4(1.0f, 1.0f, 1.0f, 0.9f), min(dis/3.0f, 1.0f));
	}
	outGlow = vec4(0.5f,0.5f,0.5f,0.0f);
}