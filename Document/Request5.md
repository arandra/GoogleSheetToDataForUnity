# GoogleSheetToDataForUnity 준비 요청서 (Request #5)

현재는 생성된 data asset에 fileId와 시트이름을 직렬화 하는 방식이다. 
이 경우 공개되는데이터에 file id가 포함되어 좋지않아 보인다.
생성된 data asset과 script들을 관리를 위한 editor 용 so를 별도로 생성한다.
해당 so를 활용한 ui를 새로 만들거나, 기존 생성용 ui에 통합한다.

추가되는 기능
- 더 이상 사용하지 않는 asset과 script들을 삭제하는 기능.
- 관련 google sheet을 web browser를 통해 열어보는 기능.
- google sheet의 변경사항을 다시 down 받아 적용하는 기능.
  * 순수 data만 변경 되지 않고 필드 추가/삭제가 있는 경우 재생성 할 것인지 묻는 절차 추가.
 