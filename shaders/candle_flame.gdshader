shader_type spatial;
render_mode cull_disabled, depth_draw_opaque;

// Vertex
uniform float flame_height = 1.8;
uniform float taper_factor: hint_range(0.0, 1.0, 0.1) = 1.0;
uniform vec3 deviation = vec3(0.0, 0.0, 0.0);
uniform float turbulence_speed: hint_range(0.0, 30.0, 1.0) = 3.0;
uniform float turbulence_force: hint_range(0.0, 100.0, 1.0) = 2.0;

// Fragment
uniform vec3 inner_color: source_color = vec3(1.0, 1.0, 1.0);
uniform vec3 outer_color: source_color = vec3(1.0, 0.75, 0.0);
uniform vec3 bottom_color: source_color = vec3(0.0, 0.1, 1.0);
uniform float emission: hint_range(1.0, 50.0, 0.1) = 5.0;
uniform float max_alpha: hint_range(0.0, 1.0, 0.01) = 0.4;
uniform float border_width: hint_range(0.0, 1.0, 0.01) = 0.75;
uniform float bottom_color_fade_y_end: hint_range(0.0, 1.0, 0.01) = 0.2;
uniform float bottom_color_fade_y_start: hint_range(0.0, 1.0, 0.01) = 0.6;

void vertex() {
	vec3 _deviation = vec3(deviation.x, 0, deviation.z) * 0.2;
	float height = (1.0 - UV.y);

	VERTEX = mix(VERTEX, vec3(0.0, VERTEX.y, 0.0), height * taper_factor) + _deviation * height;
	VERTEX.x += cos(TIME * turbulence_speed + VERTEX.y * PI) * 0.0015 * turbulence_force;
	VERTEX.y *= flame_height;
}

void fragment() {
	float height = 1.0 - UV.y;
	float vision_dot = dot(normalize(NORMAL), normalize(VIEW));
	float edge = 1.0 - vision_dot;

	float height_fade = smoothstep(0.0, 0.65, height);
	height_fade = pow(height_fade, 4.0);

	ALPHA = height_fade - pow(vision_dot, 3.0) * 0.1;
	ALPHA = mix(edge, ALPHA, pow(UV.y, 0.05));

	ALPHA *= pow(vision_dot, 1.5);
	ALPHA = clamp(ALPHA, 0.0, max_alpha);

	ALBEDO = mix(inner_color, outer_color, pow(edge, 1.0 - border_width) + 0.5 - UV.y);

	ALBEDO = mix(ALBEDO, bottom_color, smoothstep(bottom_color_fade_y_start, bottom_color_fade_y_end, height));

	EMISSION = ALBEDO * vision_dot * emission;
}