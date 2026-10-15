/* Offline Three.js viewport. Grid (x,y,elevation) maps to world (x,elevation*0.45,-y). */
(function () {
  'use strict';
  class CombatViewport {
    constructor(host, callbacks) {
      this.host = host; this.callbacks = callbacks; this.items = []; this.labels = []; this.yaw = Math.PI / 5; this.pitch = .9; this.distance = 18;
      this.target = new THREE.Vector3(); this.drag = null; this.showLabels = true; this.scheduled = false;
      this.scene = new THREE.Scene(); this.scene.background = new THREE.Color('#182a37');
      this.camera = new THREE.PerspectiveCamera(38, 1, .1, 500);
      this.renderer = new THREE.WebGLRenderer({ antialias: true, alpha: false });
      this.renderer.setPixelRatio(Math.min(window.devicePixelRatio || 1, 2));
      this.renderer.outputColorSpace = THREE.SRGBColorSpace;
      this.renderer.domElement.setAttribute('aria-label', '3D 전투 레벨'); this.renderer.domElement.tabIndex = 0;
      host.prepend(this.renderer.domElement);
      this.scene.add(new THREE.HemisphereLight(0xd6efff, 0x1f303d, 2.2));
      const sun = new THREE.DirectionalLight(0xfff1dc, 2.5); sun.position.set(-5, 14, 8); this.scene.add(sun);
      this.map = new THREE.Group(); this.scene.add(this.map);
      this.geometries = {
        box: new THREE.BoxGeometry(.95, 1, .95), top: new THREE.PlaneGeometry(.95, .95),
        base: new THREE.CylinderGeometry(.29, .29, .12, 24), body: new THREE.CapsuleGeometry(.19, .48, 6, 16),
        rail: new THREE.BoxGeometry(.05, .045, .93), sleeper: new THREE.BoxGeometry(.66, .035, .08)
      };
      this.materials = new Map(); this.raycaster = new THREE.Raycaster(); this.pointer = new THREE.Vector2();
      this.rangeMaterials = {
        move: new THREE.MeshBasicMaterial({color:0x46d2a5,transparent:true,opacity:.58,depthWrite:false}),
        attack: new THREE.MeshBasicMaterial({color:0xffab55,transparent:true,opacity:.58,depthWrite:false}),
        both: new THREE.MeshBasicMaterial({color:0xf3db65,transparent:true,opacity:.65,depthWrite:false})
      };
      this.selection = new THREE.LineSegments(new THREE.EdgesGeometry(new THREE.BoxGeometry(1, .05, 1)), new THREE.LineBasicMaterial({ color: 0xd9ffd7 }));
      this.scene.add(this.selection); this.selection.visible = false;
      this.resizeObserver = new ResizeObserver(() => this.resize()); this.resizeObserver.observe(host);
      const canvas = this.renderer.domElement;
      canvas.addEventListener('contextmenu', e => e.preventDefault());
      canvas.addEventListener('pointerdown', e => this.pointerDown(e));
      canvas.addEventListener('pointermove', e => this.pointerMove(e));
      canvas.addEventListener('pointerup', e => this.pointerUp(e));
      canvas.addEventListener('pointercancel', e => this.pointerUp(e));
      canvas.addEventListener('lostpointercapture', e => this.pointerUp(e));
      canvas.addEventListener('wheel', e => { e.preventDefault(); this.distance = THREE.MathUtils.clamp(this.distance * Math.exp(e.deltaY * .001), 3, 140); this.draw(); }, { passive: false });
      host.editorView = this;
      this.resize();
    }
    material(color) {
      if (!this.materials.has(color)) this.materials.set(color, new THREE.MeshStandardMaterial({ color, roughness: .8, metalness: .05 }));
      return this.materials.get(color);
    }
    mesh(geometry, color, x, y, z, parent = this.map) {
      const mesh = new THREE.Mesh(this.geometries[geometry], this.material(color)); mesh.position.set(x, y, z); parent.add(mesh); return mesh;
    }
    resize() {
      const w = this.host.clientWidth, h = this.host.clientHeight;
      if (!w || !h) return;
      this.lastWidth = w; this.lastHeight = h;
      this.renderer.setSize(w, h, false); this.camera.aspect = w / h; this.camera.updateProjectionMatrix(); this.draw();
    }
    update(state, selected, characterOf, ranges = {move:[],attack:[]}, methodName = ()=>'방식 미지정') {
      this.state = state; this.map.clear(); this.items = [];
      const s = state.stage, cellMap = new Map(state.cells.map(c => [`${c.x},${c.y}`, c]));
      if (this.mapSize !== `${s.width},${s.height}`) { this.mapSize = `${s.width},${s.height}`; this.resetCamera(false); }
      const labelHost = document.getElementById('viewport-labels'); labelHost.replaceChildren(); this.labels = [];
      for (const c of state.cells) {
        const color = c.type === 'Blocked' ? '#45505b' : c.type === 'Rail' ? '#8d8270' : c.y < s.playerDepth ? '#408b86' : c.y >= s.height - s.enemyDepth ? '#ae7371' : '#718793';
        const height = c.elevation * .45 + .2, block = this.mesh('box', color, c.x, height / 2 - .2, -c.y);
        block.scale.y = height; block.userData = { x: c.x, y: c.y }; this.items.push(block);
        if (c.type === 'Blocked') {
          const wall = this.mesh('box', '#353e47', c.x, c.elevation * .45 + .2, -c.y); wall.scale.set(.78, .4, .78); wall.userData = { x: c.x, y: c.y }; this.items.push(wall);
        } else if (c.type === 'Rail') {
          this.mesh('rail', '#d7c6a4', c.x - .23, c.elevation * .45 + .04, -c.y);
          this.mesh('rail', '#d7c6a4', c.x + .23, c.elevation * .45 + .04, -c.y);
          for (const z of [-.3, 0, .3]) this.mesh('sleeper', '#60543e', c.x, c.elevation * .45 + .015, -c.y + z);
        }
      }
      for (const u of state.units) {
        const character = characterOf(u.characterKey), D = window.CombatReferences;
        const base = cellMap.get(`${u.x},${u.y}`).elevation * .45, color = character?.color || D.colorOf(u.characterKey);
        const group = new THREE.Group(); group.position.set(u.x, base, -u.y); this.map.add(group);
        this.mesh('base', u.owner === 'Player' ? '#17655f' : '#823e3b', 0, .07, 0, group);
        this.mesh('body', color, 0, .55, 0, group);
        group.children.forEach(m => { m.userData = { x: u.x, y: u.y }; this.items.push(m); });
        const label = document.createElement('span'); label.className = 'world-label'; label.style.borderLeftColor = color;
        label.classList.toggle('selected',selected?.x===u.x&&selected?.y===u.y);
        const title = document.createElement('strong'); title.textContent = `${character?.name || `Character ${u.characterKey}`} · ${character?.roleName || '직업 미지정'} #${u.key}`;
        const stats = document.createElement('small'); stats.textContent = `생명력 ${D.show(D.value(character,'HealthValue'))} / 방어력 ${D.show(D.value(character,'DefenseValue'))} / 공격력 ${D.show(D.value(character,'AttackValue'))}`;
        const method = document.createElement('small'); method.className = 'label-method';
        method.textContent = `이동 ${D.show(D.value(character,'MoveRange'))}칸 / 공격 ${D.show(D.value(character,'AttackRange'))}칸 · ${methodName(D.value(character,'AttackMethod'))}`;
        label.append(title,stats,method); labelHost.append(label);
        this.labels.push({ element: label, point: new THREE.Vector3(u.x, base + 1.05, -u.y) });
      }
      const move = new Set(ranges.move.map(c=>`${c.x},${c.y}`)), attack = new Set(ranges.attack.map(c=>`${c.x},${c.y}`));
      for(const p of new Set([...move,...attack])){
        const c = cellMap.get(p); if(!c)continue;
        const material = move.has(p) && attack.has(p) ? this.rangeMaterials.both : move.has(p) ? this.rangeMaterials.move : this.rangeMaterials.attack;
        const overlay = new THREE.Mesh(this.geometries.top,material); overlay.rotation.x = -Math.PI/2;
        overlay.position.set(c.x,c.elevation*.45 + (c.type==='Blocked' ? .42 : .025),-c.y); this.map.add(overlay);
      }
      this.selection.visible = Boolean(selected);
      if (selected) { const cell = cellMap.get(`${selected.x},${selected.y}`); if (cell) this.selection.position.set(cell.x, cell.elevation * .45 + .025, -cell.y); else this.selection.visible = false; }
      this.draw();
    }
    resetCamera(redraw = true) {
      const s = this.state?.stage || { width: 8, height: 10 };
      const maxHeight = Math.max(0, ...(this.state?.cells || []).map(c => c.elevation * .45));
      this.target.set((s.width - 1) / 2, maxHeight / 3, -(s.height - 1) / 2);
      this.yaw = Math.PI / 5; this.pitch = .9;
      const aspect = (this.host.clientWidth || this.lastWidth || 600) / (this.host.clientHeight || this.lastHeight || 500);
      this.distance = Math.max(s.width / Math.min(1, aspect), s.height, maxHeight * 1.7, 4) * 1.9;
      if (redraw) this.draw();
    }
    cameraPosition() {
      this.camera.position.set(this.target.x + this.distance * Math.sin(this.yaw) * Math.cos(this.pitch), this.target.y + this.distance * Math.sin(this.pitch), this.target.z + this.distance * Math.cos(this.yaw) * Math.cos(this.pitch));
      this.camera.lookAt(this.target); this.camera.updateMatrixWorld();
    }
    draw() {
      if (this.scheduled) return; this.scheduled = true;
      requestAnimationFrame(() => {
        this.scheduled = false; this.cameraPosition(); this.renderer.render(this.scene, this.camera);
        const w = this.host.clientWidth, h = this.host.clientHeight;
        for (const label of this.labels) {
          const p = label.point.clone().project(this.camera);
          label.element.hidden = !this.showLabels || p.z < -1 || p.z > 1 || Math.abs(p.x) > 1.1 || Math.abs(p.y) > 1.1;
          label.element.style.left = `${(p.x + 1) * w / 2}px`; label.element.style.top = `${(1 - p.y) * h / 2}px`;
        }
      });
    }
    hit(e) {
      const r = this.renderer.domElement.getBoundingClientRect();
      this.pointer.set((e.clientX - r.left) / r.width * 2 - 1, -(e.clientY - r.top) / r.height * 2 + 1);
      this.cameraPosition(); this.map.updateMatrixWorld(true); this.raycaster.setFromCamera(this.pointer, this.camera);
      return this.raycaster.intersectObjects(this.items, false)[0]?.object.userData || null;
    }
    pointerDown(e) {
      this.renderer.domElement.focus(); this.renderer.domElement.setPointerCapture(e.pointerId);
      const orbit = e.button === 2 || e.button === 1;
      this.drag = { id: e.pointerId, orbit, pan: e.shiftKey, x: e.clientX, y: e.clientY, last: null };
      if (orbit) return;
      if (e.button !== 0) { this.drag = null; return; }
      this.callbacks.start(); const hit = this.hit(e);
      if (hit) { this.drag.last = `${hit.x},${hit.y}`; this.callbacks.edit(hit.x, hit.y, false); }
    }
    pointerMove(e) {
      const d = this.drag;
      if (d?.orbit) {
        const dx = e.clientX - d.x, dy = e.clientY - d.y;
        if (d.pan) {
          const scale = this.distance / 650;
          this.target.x -= (dx * Math.cos(this.yaw) + dy * Math.sin(this.yaw)) * scale;
          this.target.z += (dx * Math.sin(this.yaw) - dy * Math.cos(this.yaw)) * scale;
        } else { this.yaw -= dx * .008; this.pitch = THREE.MathUtils.clamp(this.pitch + dy * .006, .15, 1.48); }
        d.x = e.clientX; d.y = e.clientY; this.draw(); return;
      }
      const hit = this.hit(e); if (hit) this.callbacks.hover(hit.x, hit.y);
      if (d && hit && d.last !== `${hit.x},${hit.y}`) { d.last = `${hit.x},${hit.y}`; this.callbacks.edit(hit.x, hit.y, true); }
    }
    pointerUp(e) {
      if (!this.drag || this.drag.id !== e.pointerId) return;
      if (!this.drag.orbit) this.callbacks.end(); this.drag = null;
    }
    projectCell(x, y) {
      // Also useful for placing overlays / integration tests without hard-coded screen coordinates.
      const c = this.state.cells.find(c => c.x === x && c.y === y);
      this.cameraPosition(); const p = new THREE.Vector3(x, c.elevation * .45 + .01, -y).project(this.camera);
      const r = this.renderer.domElement.getBoundingClientRect(); return { x: r.left + (p.x + 1) * r.width / 2, y: r.top + (1 - p.y) * r.height / 2 };
    }
  }
  class CapsulePreview {
    constructor(){
      this.renderer=new THREE.WebGLRenderer({antialias:true,alpha:true,preserveDrawingBuffer:true});
      this.renderer.outputColorSpace=THREE.SRGBColorSpace;
      this.scene=new THREE.Scene();this.scene.add(new THREE.HemisphereLight(0xe2f8ff,0x55716a,2.5));
      const sun=new THREE.DirectionalLight(0xfff4dc,3);sun.position.set(-3,5,3);this.scene.add(sun);
      this.material=new THREE.MeshStandardMaterial({color:0x56c5b8,roughness:.55});
      const capsule=new THREE.Mesh(new THREE.CapsuleGeometry(.22,.55,6,20),this.material);capsule.position.y=.59;this.scene.add(capsule);
      const base=new THREE.Mesh(new THREE.CylinderGeometry(.34,.34,.1,32),new THREE.MeshStandardMaterial({color:0x526c69,roughness:.7}));base.position.y=.05;this.scene.add(base);
      this.camera=new THREE.PerspectiveCamera(34,1,.1,20);this.camera.position.set(1.7,1.5,2.2);this.camera.lookAt(0,.52,0);
    }
    image(color,width,height){
      this.material.color.set(color);this.camera.aspect=width/height;this.camera.updateProjectionMatrix();this.renderer.setSize(width*2,height*2,false);this.renderer.render(this.scene,this.camera);
      const canvas=document.createElement('canvas');canvas.width=width*2;canvas.height=height*2;canvas.style.width=`${width}px`;canvas.style.height=`${height}px`;canvas.setAttribute('aria-label','선택 색상의 3D 캡슐');canvas.setAttribute('role','img');canvas.getContext('2d').drawImage(this.renderer.domElement,0,0);return canvas;
    }
  }
  window.CombatViewport = CombatViewport;
  window.CapsulePreview = CapsulePreview;
})();
