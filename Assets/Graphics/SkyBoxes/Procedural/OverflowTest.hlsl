#ifndef MYHLSLINCLUDE_INCLUDED
#define MYHLSLINCLUDE_INCLUDED

void OverflowTest_float(float2 UV, out float Output)
{
    Output = UV.x > 1 || UV.g > 1;

}

#endif //MYHLSLINCLUDE_INCLUDED