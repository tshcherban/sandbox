#include <avr/io.h>
#define setbit(var,bit) (var |= (0x01 << (bit)))
#define clrbit(var,bit) (var &= (~(0x01 << (bit))))

int main(void)
{
    asm("nop\n");
    asm("nop\n");
    asm("nop\n");
    asm("nop\n");

    //CLKPR = 0b10000000;
    //CLKPR = 0b00000001;
    //const unsigned long delayDuration = 2285713UL; // ~7 cycles per 1 value
    const unsigned long delayDuration = 228570UL; // ~7 cycles per 1 value

    DDRB |= 0b00100000;
    while (1)
    {
        setbit(PORTB, PINB5);
        for (unsigned long i = 0; i < delayDuration; ++i)
        {
            asm("nop\n");
        }
        asm("nop\n");
        asm("nop\n");
        asm("nop\n");
        asm("nop\n");
        
        clrbit(PORTB, PINB5);
        for (unsigned long i = 0; i < delayDuration; ++i)
        {
            asm("nop\n");
        }
        asm("nop\n");
        asm("nop\n");
        asm("nop\n");
        asm("nop\n");
    }
}
