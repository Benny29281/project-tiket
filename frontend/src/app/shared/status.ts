import { Component, input } from '@angular/core';
import { Status, statusLabels } from '../core/models';
@Component({ selector: 'hd-status', template: `<span [class]="'badge status-' + value()"><i></i>{{ labels[value()] }}</span>` })
export class StatusBadge { value = input.required<Status>(); labels = statusLabels; }
