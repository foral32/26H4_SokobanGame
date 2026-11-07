export const directions=[[0,1],[1,0],[0,-1],[-1,0]];
export const directionNames=['上','右','下','左'];
export const maskNames=['None','Up','Right','UpRight','Down','UpDown','RightDown','UpRightDown','Left','UpLeft','RightLeft','UpRightLeft','DownLeft','UpDownLeft','RightDownLeft','Uprightdownleft'];
export const cornerNames=['UpRight','DownRight','DownLeft','UpLeft'];
export const missingMask=connected=>connected.reduce((mask,present,i)=>present?mask:mask|(1<<i),0);
export const cornerFlags=(cardinal,diagonal,active=true)=>cardinal.map((present,i)=>active&&present&&cardinal[(i+1)%4]&&!diagonal[i]);
