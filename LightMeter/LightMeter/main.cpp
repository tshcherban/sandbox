#include <avr/io.h>
#include <avr/interrupt.h>

// pins for debugging purposes
#define UART_PIN PIND5
#define ADC_PIN PIND4

// timer 0 clock sources
#define T0CL_0 0
#define T0CL_1 1
#define T0CL_8 2
#define T0CL_64 3
#define T0CL_256 4
#define T0CL_1024 5
#define T0CL_EXT_FALL 6
#define T0CL_EXT_RISE 7

#define SET_BIT(BYTE, BIT) BYTE |= (1 << BIT)
#define CLR_BIT(BYTE, BIT) BYTE &= ~(1 << BIT)
#define TST_BIT(BYTE, BIT) (BYTE & (1 << BIT))

// timer 0 double frequency (interrupt trigger frequency), calculated for clock prescaler 64 and ctc mode
#define FREQ_15_625 15
#define FREQ_12_5 19
#define FREQ_10_0 24
#define FREQ_6_25 39
#define FREQ_5_0 49
#define FREQ_2_0 124
#define FREQ_1_0 249

inline void setup();

char adc;
bool allowRun;
ISR(TIMER0_COMPA_vect)
{
    SET_BIT(PORTD, UART_PIN);
    SET_BIT(ADCSRA, ADSC);
    UDR0 = adc;
    allowRun = true;
    CLR_BIT(PORTD, UART_PIN);
}

int main(void)
{
    setup();

    while (1)
    {
        while (true) {
            while (!allowRun);
            while (TST_BIT(ADCSRA, ADSC));
            adc = ADCH;
            allowRun = false;
        }
    }
}

void setup()
{
    DDRD |= (1 << ADC_PIN) | (1 << UART_PIN) | (1 << PIND6);

    UCSR0B = (1 << RXEN0) | (1 << TXEN0);
    UCSR0C = (1 << USBS0) | (3 << UCSZ00);
    UCSR0A = (1 << U2X0);
    UBRR0L = 0;
    UBRR0H = 0;

    ADMUX = (1 << ADLAR) | (1 << REFS1) | (1 << REFS0);
    ADCSRA = (1 << ADEN) | (1 << ADSC) | (1 << ADPS2) | (1 << ADPS1);
    
    TCCR0A = (1 << COM0A0) | (1 << WGM01);
    TCCR0B = (T0CL_64 << CS00);
    TIMSK0 = (1 << OCIE0A);
    OCR0A = FREQ_15_625;
    TCNT0 = 0;
    sei();
}