language: pl
Potrzeba biznesowa: Operacje na sesji wsadowej

@smoke @Batch @regresja @KSEF20-460
Scenariusz: Wys�anie dokument�w w jednocz�ciowej paczce
Zak�adaj�c, �e przygotowa�em paczk� dokument�w gotowych do wys�ania w sesji wsadowej
Je�eli je�eli nawi��� sesj� wsadow�
Oraz wy�l� paczk� dokument�w na wskazany adres
Oraz zako�cz� sesj� wsadow�
Wtedy dokumenty zostan� przetworzone i zostan� na ich podstawie wystawione faktury
Oraz zostanie to potwierdzone wygenerowaniem dokumentu UPO

@smoke @Batch @regresja @Negative @KSEF20-459
Scenariusz: Wys�anie jednocz�ciowej paczki dokument�w ze z�ym NIP
Zak�adaj�c, �e przygotowa�em paczk� dokument�w z nieprawid�owym NIP gotowych do wys�ania w sesji wsadowej
Je�eli je�eli nawi��� sesj� wsadow�
Oraz wy�l� paczk� dokument�w na wskazany adres
Oraz zako�cz� sesj� wsadow�
Wtedy proces wystawiania faktury zako�czy si� niepowodzeniem