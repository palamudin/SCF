inline float3 ChangeHue(float3 aColor, float aHue) {
    float angle = radians(aHue);
    float3 i = float3(0.57735, 0.57735, 0.57735);
    float cosAngle = cos(angle);
    return aColor * cosAngle + cross(i, aColor) * sin(angle) + i * dot(i, aColor) * (1 - cosAngle);
}
 
inline float4 ChangeColor(float4 color, fixed4 hsbc) {
	
    float4 ocol = color;
    ocol.rgb = ChangeHue(ocol.rgb, 360 * hsbc.r);
    ocol.rgb = (ocol.rgb - 0.5f) * (hsbc.a) + 0.5f;
    ocol.rgb = ocol.rgb + hsbc.b - 1.0;
    float3 f = dot(ocol.rgb, float3(0.299,0.587,0.114));
    ocol.rgb = lerp(f, ocol.rgb, hsbc.g);
 
    return ocol;
}