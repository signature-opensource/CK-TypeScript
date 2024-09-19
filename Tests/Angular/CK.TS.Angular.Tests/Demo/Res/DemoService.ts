
import { Injectable } from '@angular/core';

@Injectable({ providedIn: 'root' })
export class GoodLuckService {
    getLuck() {
        return "Today is your lucky day.";
    }
}
