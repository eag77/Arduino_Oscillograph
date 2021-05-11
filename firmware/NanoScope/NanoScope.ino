const int analogInPin = A0;
const int ledPin = 13;

unsigned long Period = 500;

#define length 512
union u16_8
{
	uint16_t buf16[length + 7];
	uint8_t buf8[(length + 7) * 2];
};
u16_8 BUF;
uint16_t BUF2[length/2];
volatile unsigned long time;
uint16_t posbuf = 0;		//Текущая позиция в буфере
uint8_t StateSend = 0;		//
bool adce = false;			//Запись даных АЦП в буфер закончена
uint16_t TrigerV = 512;		//Порог тригера
uint16_t TrigerPos;			//Позиция тригера	(256)
uint16_t TrigerPosS;		//Позиция срабатывания тригера
uint16_t CountEnd;			//Счетчик выборок до конца
bool bTrigerF = true;		//Тригер по фронту иначе по спаду
uint8_t TrigerState = 0;	//
uint16_t Data = 0;			//Значение АЦП
uint16_t DataOld = 0;		//Предыдущее значение АЦП

#define ADCADPS 6
#define ADCSTART ADCSRA = (1 << ADEN) | (1 << ADSC) | (1 << ADATE) | (1 << ADIE) | (ADCADPS << ADPS0);
#define ADCSTOP ADCSRA = (1 << ADEN) | (0 << ADSC) | (1 << ADATE) | (0 << ADIE) | (ADCADPS << ADPS0);

void setup() {
//	Serial.begin(115200, SERIAL_8N2);
	Serial.begin(1024000, SERIAL_8N2);
	pinMode(ledPin, OUTPUT);
	time = Period + millis();
}

void loop() {
	SendData();
}

void SendData()
{
	switch (StateSend)
	{
	case 0:
		if (time > millis())
			return;
		time = Period + millis();
		digitalWrite(ledPin, HIGH);
		posbuf = 0;
		TrigerState = 0;
		ADMUX = (1 << REFS0) | (0 << ADLAR) | (0 << MUX0);
//		DIDR0 = (0 << ADC5D) | (0 << ADC4D) | (0 << ADC3D) | (0 << ADC2D) | (0 << ADC1D) | (1 << ADC0D);
		ADCSRB = (0 << ADTS0);
		ADCSRA = (1 << ADEN) | (0 << ADSC) | (0 << ADATE) | (1 << ADIE) | (0 << ADPS0);
//		ADCSRA = (1 << ADEN) | (1 << ADSC) | (0 << ADATE) | (1 << ADIE) | (0 << ADPS0);
		ADCSTART;
		StateSend++;
		break;
	case 1:
		if (TrigerState != 3)
			return;
		if (TrigerPosS >= (length / 2))
		{
			memcpy(BUF2, (BUF.buf16 + TrigerPosS), length - TrigerPosS);
			memcpy(BUF.buf16, (BUF.buf16 + TrigerPosS), length - TrigerPosS);
			memcpy((BUF.buf16 + TrigerPosS), BUF2, length - TrigerPosS);
		}
		else
		{
		}
		BUF.buf8[0] = 'B';
		BUF.buf8[1] = 'e';
		BUF.buf8[2] = 'g';
		BUF.buf8[3] = 'i';
		BUF.buf8[4] = 'n';
		BUF.buf8[5] = ':';
		BUF.buf16[3] = length;
		BUF.buf16[length + 4] = TrigerPosS;
//		BUF.buf8[length * 2 + 8] = 0;
//		BUF.buf8[length * 2 + 9] = 'E';
		BUF.buf8[length * 2 + 10] = 'n';
		BUF.buf8[length * 2 + 11] = 'd';
		BUF.buf8[length * 2 + 12] = 0x0d;
		BUF.buf8[length * 2 + 13] = 0x0a;
		StateSend++;
		break;
	case 2:
		digitalWrite(ledPin, LOW);
		Serial.flush();
		Serial.write(BUF.buf8, (length + 7) * 2);
		StateSend = 0;
		break;
	default:
		break;
	}
}

ISR(ADC_vect)
{
	Data = ADC;
	BUF.buf16[posbuf + 4] = Data;

//Определяем что буфер заполнен перед позицией триггера
	if ((TrigerState == 0) && (posbuf >= TrigerPos))
		TrigerState = 1;

//Событие срабатывания тригера
	if ((TrigerState == 1) && (((bTrigerF) && (Data > DataOld) && (Data >= TrigerV) && (DataOld < TrigerV)) ||
		((!bTrigerF) && (Data < DataOld) && (Data <= TrigerV) && (DataOld > TrigerV))))
	{
		TrigerState = 2;
		CountEnd = TrigerPos;
		TrigerPosS = posbuf;
	}

//инкремент позиции кольцевого буфера
	if (posbuf >= length)
		posbuf = 0;
	else
		posbuf++;

//Принудительный запуск триггера если небыло события срабатывания
	if ((TrigerState == 1) && (posbuf == 0))
	{
		TrigerState = 2;
		CountEnd = 0;
		TrigerPosS = 0;
	}

//Считаем выборки до окончания записи
	if (TrigerState == 2)
	{
		CountEnd++;
		if (CountEnd >= length)
		{
			TrigerState = 3;
			ADCSTOP;
			return;
		}
	}

	ADCSTART;
}

/*ISR(ADC_vect)
{
	//	uint16_t data = ADC;
	BUF.buf16[posbuf + 4] = ADC;// data;
	if (posbuf < length)
	{
		ADCSTART;
		posbuf++;
		if (!bTriger)
		{
			BUF.buf16[posbuf + 4];
		}
	}
	else
	{
		ADCSTOP;
		posbuf = 0;
		adce = true;
	}
}
*/
