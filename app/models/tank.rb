class Tank < ApplicationRecord
	belongs_to :nation
	belongs_to :vehicle_type
	has_many :marks
end
